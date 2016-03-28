using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using SharpDX;
using EloBuddy.SDK.Rendering;

namespace Riven
{
    public static class Program
    {

        private static Spell.Targeted ignite;
        private static Menu Menu;
        private const string IsFirstR = "RivenFengShuiEngine";
        private const string IsSecondR = "RivenIzunaBlade";
        public static SpellSlot Ignite;
        private static int _qStack = 1;
        public static bool CastR2;
        public static string AssemblyName = "Riven";
        private static Spell.Skillshot Q, R2, E, Flash;
        private static Spell.Active W, R1;
        private static bool forceW;
        private static bool forceQ;
        private static bool forceR;
        private static bool forceR2;
        public static bool forceQ2 = false;
        private static bool forceItem;
        private static int lastW;
        private static AttackableUnit QTarget;

        private static AIHeroClient myHero
        {
            get { return Player.Instance; }
        }

        public static float cQ;
        public static uint WRange
        {
            get
            {
                return (uint)
                        (70 + ObjectManager.Player.BoundingRadius +
                         (ObjectManager.Player.HasBuff("RivenFengShuiEngine") ? 195 : 120));
            }
        }

        private static Orbwalker.ActiveModes Mode
        {
            get { return Orbwalker.ActiveModesFlags; }
        }

        public static void Main()
        {
            Loading.OnLoadingComplete += OnLoad;
        }

        private static void AutoUseW()
        {
            if (AutoW > 0)
            {
                if (myHero.CountEnemiesInRange(W.Range) >= AutoW)
                {
                    W.Cast();
                }
            }
        }

        private static readonly float _barLength = 104;
        private static readonly float _xOffset = 2;
        private static readonly float _yOffset = 9;
        private static void Drawing_OnEndScene(EventArgs args)
        {
            if (myHero.IsDead)
                return;
            if (!Dind) return;
            foreach (var aiHeroClient in EntityManager.Heroes.Enemies)
            {
                if (!aiHeroClient.IsHPBarRendered || !aiHeroClient.VisibleOnScreen) continue;

                var pos = new Vector2(aiHeroClient.HPBarPosition.X + _xOffset, aiHeroClient.HPBarPosition.Y + _yOffset);
                var fullbar = (_barLength) * (aiHeroClient.HealthPercent / 100);
                var damage = (_barLength) *
                                 ((getComboDamage(aiHeroClient) / aiHeroClient.MaxHealth) > 1
                                     ? 1
                                     : (getComboDamage(aiHeroClient) / aiHeroClient.MaxHealth));
                Line.DrawLine(System.Drawing.Color.Aqua, 9f, new Vector2(pos.X, pos.Y),
                    new Vector2(pos.X + (damage > fullbar ? fullbar : damage), pos.Y));
                Line.DrawLine(System.Drawing.Color.Black, 9, new Vector2(pos.X + (damage > fullbar ? fullbar : damage) - 2, pos.Y), new Vector2(pos.X + (damage > fullbar ? fullbar : damage) + 2, pos.Y));
            }
        }


        private static void ComboLogic()
        {
            if (ComboW)
            {
                var t = EntityManager.Heroes.Enemies.Find(x => x.IsValidTarget(W.Range) && !x.HasBuffOfType(BuffType.SpellShield));

                if (t != null)
                {
                    if (W.IsReady() && !Orbwalker.CanAutoAttack)
                    {
                        W.Cast();
                    }
                }
            }

            if (E.IsReady())
            {
                var t = EntityManager.Heroes.Enemies.Where(e => e.IsValidTarget(E.Range + myHero.GetAutoAttackRange()));

                if (t == null)
                {
                    return;
                }

                var t12 = t.OrderByDescending(e => TargetSelector.GetPriority(e)).FirstOrDefault();

                if (t12 != null)
                {
                    if (myHero.Distance(t12) > myHero.GetAutoAttackRange() + 20)
                    {
                        E.Cast(t12.ServerPosition);
                    }
                }
                if (ComboE == 0)
                {
                    var t1 = t.OrderByDescending(e => TargetSelector.GetPriority(e)).FirstOrDefault();

                    if (t1 != null)
                        E.Cast(t1.ServerPosition);
                }
                else if (ComboE == 1)
                {
                    if (t != null)
                        E.Cast(Game.CursorPos);
                }
            }

            if (AlwaysR)
            {
                if (R1.IsReady())
                {
                    if (AlwaysR && !myHero.HasBuff("RivenFengShuiEngine"))
                    {
                        var t = TargetSelector.GetTarget(900, DamageType.Physical);
                        if (t == null)
                        {
                            return;
                        }
                        if (t.Distance(myHero.ServerPosition) < E.Range + myHero.AttackRange && myHero.CountEnemiesInRange(500) >= 1)
                            R1.Cast();
                    }

                    if (myHero.HasBuff("RivenFengShuiEngine"))
                    {
                        var t = TargetSelector.GetTarget(900, DamageType.Physical);
                        if (t == null)
                        {
                            return;
                        }
                        if (t.ServerPosition.Distance(myHero.ServerPosition) < 850)
                        {
                            switch (R2Mode)
                            {
                                case 0:
                                    if (Rdame(t, t.Health) > t.Health && t.IsValidTarget(R2.Range) && t.Distance(myHero.ServerPosition) < 600)
                                    {
                                        CastR2 = true;
                                    }
                                    else
                                    {
                                        CastR2 = false;
                                    }
                                    break;
                                case 1:
                                    var prediction = R2.GetPrediction(t);
                                    if (t.HealthPercent < 25 && t.Health > Rdame(t, t.Health) + Damage.GetAutoAttackDamage(myHero, t) * 2)
                                    {
                                        R2.Cast(prediction.CastPosition);
                                    }
                                    else
                                    {
                                        CastR2 = false;
                                    }
                                    break;
                                case 2:
                                    if (t.IsValidTarget(R2.Range) && t.Distance(myHero.ServerPosition) < 600)
                                    {
                                        CastR2 = true;
                                    }
                                    else
                                    {
                                        CastR2 = false;
                                    }
                                    break;
                                case 3:
                                    CastR2 = false;
                                    break;
                            }
                        }

                        if (CastR2)
                        {
                            R2.Cast(t);
                        }
                    }
                }
            }
        }

        #region Menu Items
        //public static bool useQ { get { return Menu["useQ"].Cast<CheckBox>().CurrentValue; } }
        public static int AutoW { get { return Menu["AutoW"].Cast<Slider>().CurrentValue; } }
        public static bool ComboW { get { return Menu["ComboW"].Cast<CheckBox>().CurrentValue; } }
        public static bool AutoShield { get { return Menu["AutoShield"].Cast<CheckBox>().CurrentValue; } }
        public static bool Shield { get { return Menu["Shield"].Cast<CheckBox>().CurrentValue; } }
        public static bool Winterrupt { get { return Menu["Winterrupt"].Cast<CheckBox>().CurrentValue; } }
        public static int R2Mode { get { return Menu["R2Mode"].Cast<Slider>().CurrentValue; } }
        public static bool UseR1
        {
            get { return Menu["useR1"].Cast<KeyBind>().CurrentValue; }
        }
        public static int ComboE { get { return Menu["ComboE"].Cast<Slider>().CurrentValue; } }
        public static bool harassQ { get { return Menu["harassQ"].Cast<CheckBox>().CurrentValue; } }
        public static bool LaneQ { get { return Menu["LaneQ"].Cast<CheckBox>().CurrentValue; } }
        public static bool LaneW { get { return Menu["LaneW"].Cast<CheckBox>().CurrentValue; } }
        public static bool LaneE { get { return Menu["LaneE"].Cast<CheckBox>().CurrentValue; } }
        public static bool jungleQ { get { return Menu["jungleQ"].Cast<CheckBox>().CurrentValue; } }
        public static bool harassW { get { return Menu["harassW"].Cast<CheckBox>().CurrentValue; } }
        public static bool doBurst { get { return Menu["doBurst"].Cast<KeyBind>().CurrentValue; } }
        public static bool jungleW { get { return Menu["jungleW"].Cast<CheckBox>().CurrentValue; } }
        public static bool jungleE { get { return Menu["jungleE"].Cast<CheckBox>().CurrentValue; } }
        public static bool KillStealQ { get { return Menu["KillStealQ"].Cast<CheckBox>().CurrentValue; } }
        public static bool KillStealW { get { return Menu["KillStealW"].Cast<CheckBox>().CurrentValue; } }
        public static bool KillStealE { get { return Menu["KillStealE"].Cast<CheckBox>().CurrentValue; } }
        public static bool KillStealR { get { return Menu["KillStealR"].Cast<CheckBox>().CurrentValue; } }
        public static bool Flee { get { return Menu["Flee"].Cast<CheckBox>().CurrentValue; } }
        public static bool Youmu { get { return Menu["youmu"].Cast<CheckBox>().CurrentValue; } }
        public static int Q1Delay
        {
            get { return Menu["q1delay"].Cast<Slider>().CurrentValue; }
        }
        public static int Q2Delay
        {
            get { return Menu["q2delay"].Cast<Slider>().CurrentValue; }
        }
        public static int Q3Delay
        {
            get { return Menu["q3delay"].Cast<Slider>().CurrentValue; }
        }
        public static int WDelay
        {
            get { return Menu["wdelay"].Cast<Slider>().CurrentValue; }
        }
        public static bool AlwaysCancel
        {
            get { return Menu["alwayscancel"].Cast<CheckBox>().CurrentValue; }
        }
        private static bool Dind
        {
            get { return Menu["Dind"].Cast<CheckBox>().CurrentValue; }
        }

        private static bool DrawCB
        {
            get { return Menu["DrawCB"].Cast<CheckBox>().CurrentValue; }
        }

        private static bool DrawAlwaysR
        {
            get { return Menu["DrawAlwaysR"].Cast<CheckBox>().CurrentValue; }
        }
        private static bool DrawFH
        {
            get { return Menu["DrawFH"].Cast<CheckBox>().CurrentValue; }
        }
   
        private static bool AlwaysR
        {
            get { return Menu["AlwaysR"].Cast<KeyBind>().CurrentValue; }
        }

   

        /*
                private static bool DrawTimer1
                {
                    get { return menu["DrawTimer1"].Cast<CheckBox>().CurrentValue; }
                }
                private static bool DrawTimer2
                {
                    get { return menu["DrawTimer2"].Cast<CheckBox>().CurrentValue; }
                }
        */


        public static int QGapclose
        {
            get { return Menu["qgapclose"].Cast<Slider>().CurrentValue; }
        }

        #endregion

        private static void OnLoad(EventArgs args)
        {
            if (myHero.Hero != Champion.Riven)
            {
                return;
            }

            Menu = MainMenu.AddMenu("Riven", "Riven");
            Menu.AddLabel("A combination of all Riven ports Kappa");
            Menu.AddSeparator();
            Menu.AddGroupLabel("Combo");
            Menu.Add("qgapclose", new Slider("Gaplose with {0}Q", 0, 0, 3));
            Menu.AddLabel("This one will enable gapclosing with Qs.");
            Menu.AddLabel("0 means it turned off, 1 - it will use Q1, 2 - Q1 and Q2, 3 - Q1,Q2,Q3");
            Menu.AddSeparator();
            Menu.Add("AlwaysR", new KeyBind("Forced R", false, KeyBind.BindTypes.PressToggle, 'G'));
            Menu.Add("ComboW", new CheckBox("Always use W"));
            Menu.AddLabel("R2 Modes : ");
            Menu.Add("R2Mode", new Slider("0 : Killable | 1 : Max Damage | 2 : First Cast | 3 : Off", 0, 0, 3));
            Menu.AddSeparator();
            Menu.AddLabel("E Modes : ");
            Menu.Add("ComboE", new Slider("0 : To Target | 1 : To Mouse", 0, 0, 1));
            Menu.AddLabel("Q Delays : ");
            Menu.AddSeparator();
            Menu.Add("q1delay", new Slider("Q1 animation reset delay {0}ms default 293", 291, 0, 500));
            Menu.Add("q2delay", new Slider("Q2 animation reset delay {0}ms default 293", 291, 0, 500));
            Menu.Add("q3delay", new Slider("Q3 animation reset delay {0}ms default 393", 393, 0, 500));
            Menu.Add("wdelay", new Slider("W animation reset delay {0}ms default 170", 170, 0, 500));
            Menu.Add("alwayscancel", new CheckBox("Cancel animation from manual Qs"));

            Menu.AddGroupLabel("Burst Combo");
            Menu.Add("doBurst", new KeyBind("Do Burst Combo", false, KeyBind.BindTypes.HoldActive, 'T'));
            Menu.AddSeparator();

            Menu.AddGroupLabel("Harass");
            Menu.Add("harassQ", new CheckBox("Use Q"));
            Menu.Add("harassW", new CheckBox("Use W"));
            Menu.AddSeparator();

            Menu.AddGroupLabel("Lane Clear");
            Menu.Add("LaneQ", new CheckBox("Use Q While Laneclear"));
            Menu.Add("LaneW", new CheckBox("Use Q While Laneclear"));
            Menu.Add("LaneE", new CheckBox("Use E While Laneclear"));



            Menu.AddSeparator();

            Menu.AddGroupLabel("Jungle Clear");
            Menu.Add("jungleQ", new CheckBox("Use Q"));
            Menu.Add("jungleW", new CheckBox("Use W"));
            Menu.Add("jungleE", new CheckBox("Use E"));
            Menu.AddSeparator();

            Menu.AddGroupLabel("Misc");
            Menu.Add("AutoW", new Slider("Auto W When X Enemy", 5, 0, 5));
            Menu.Add("AutoShield", new CheckBox("Auto Cast E"));
            Menu.Add("Winterrupt", new CheckBox("W interrupt"));
            Menu.Add("Shield", new CheckBox("Auto Cast E While LastHit"));
            Menu.AddSeparator();
            Menu.Add("KillStealQ", new CheckBox("Use Q KS"));
            Menu.Add("KillStealW", new CheckBox("Use W KS"));
            Menu.Add("KillStealE", new CheckBox("Use E KS"));
            Menu.Add("KillStealR", new CheckBox("Use R KS"));
            Menu.AddSeparator();
            Menu.Add("youmu", new CheckBox("Use Youmys When E", false));

            Menu.AddSeparator();


            Menu.AddGroupLabel("Flee");
            Menu.Add("Flee", new CheckBox("Q/E"));
            Menu.AddSeparator();

            Menu.AddGroupLabel("Draw");
            Menu.Add("DrawAlwaysR", new CheckBox("Draw Always R Status"));
            Menu.Add("Dind", new CheckBox("Draw Damage Indicator"));
            Menu.Add("DrawCB", new CheckBox("Draw Combo Engage Range"));
            Menu.Add("DrawHS", new CheckBox("Draw Harass Engage Range"));
            Menu.AddSeparator();


            Q = new Spell.Skillshot(SpellSlot.Q, 260, SkillShotType.Circular, 250, 2200, 100);
            W = new Spell.Active(SpellSlot.W, 255);
            E = new Spell.Skillshot(SpellSlot.E, 400, SkillShotType.Linear);
            R1 = new Spell.Active(SpellSlot.R, (uint)myHero.GetAutoAttackRange());
            R2 = new Spell.Skillshot(SpellSlot.R, 900, SkillShotType.Cone, 250, 1600, 45)
            {
                MinimumHitChance = HitChance.High,
                AllowedCollisionCount = -1
            };

            var slot = Player.Instance.GetSpellSlotFromName("summonerflash");

            if (slot != SpellSlot.Unknown)
            {
                Flash = new Spell.Skillshot(slot, 800, SkillShotType.Linear);
            }

            var ign = Player.Spells.FirstOrDefault(o => o.SData.Name == "SummonerDot");


            if (ign != null)
            {
                SpellSlot igslot = EloBuddy.SDK.Extensions.GetSpellSlotFromName(myHero, "SummonerDot");

                ignite = new Spell.Targeted(igslot, 600);
            }

            Game.OnTick += OnTick;
            Obj_AI_Base.OnSpellCast += AfterAAQLogic;
            Obj_AI_Base.OnPlayAnimation += OnPlay;
            Obj_AI_Base.OnProcessSpellCast += OnCasting;
            Orbwalker.OnPostAttack += JungleClearELogic;
            Drawing.OnDraw += Drawing_OnDraw;
            Drawing.OnEndScene += Drawing_OnEndScene;

        }
        private static void Interrupt(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs e)
        {
            if (sender.IsEnemy && W.IsReady() && sender.IsValidTarget() && !sender.IsZombie && Winterrupt)
            {
                if (sender.IsValidTarget(125 + myHero.BoundingRadius + sender.BoundingRadius)) W.Cast();
            }
        }

        private static void OnCasting(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsEnemy && sender.Type == myHero.Type && AutoShield)
            {
                var epos = myHero.ServerPosition + (myHero.ServerPosition - sender.ServerPosition).Normalized() * 300;

                if (myHero.Distance(sender.ServerPosition) <= args.SData.CastRange)
                {
                    switch (args.SData.TargettingType)
                    {
                        case SpellDataTargetType.Unit:

                            if (args.Target.NetworkId == myHero.NetworkId)
                            {
                                if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LastHit) && !args.SData.Name.Contains("NasusW"))
                                {
                                    if (E.IsReady()) E.Cast(epos);
                                }
                            }

                            break;
                        case SpellDataTargetType.SelfAoe:

                            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LastHit))
                            {
                                if (E.IsReady()) E.Cast(epos);
                            }

                            break;
                    }
                    if (args.SData.Name.Contains("IreliaEquilibriumStrike"))
                    {
                        if (args.Target.NetworkId == myHero.NetworkId)
                        {
                            if (W.IsReady() && W.IsInRange(sender)) W.Cast();
                            else if (E.IsReady()) E.Cast(epos);
                        }
                    }
                    if (args.SData.Name.Contains("TalonCutthroat"))
                    {
                        if (args.Target.NetworkId == myHero.NetworkId)
                        {
                            if (W.IsReady()) W.Cast();
                        }
                    }
                    if (args.SData.Name.Contains("RenektonPreExecute"))
                    {
                        if (args.Target.NetworkId == myHero.NetworkId)
                        {
                            if (W.IsReady()) W.Cast();
                        }
                    }
                    if (args.SData.Name.Contains("GarenRPreCast"))
                    {
                        if (args.Target.NetworkId == myHero.NetworkId)
                        {
                            if (E.IsReady()) E.Cast(epos);
                        }
                    }

                    if (args.SData.Name.Contains("GarenQAttack"))
                    {
                        if (args.Target.NetworkId == myHero.NetworkId)
                        {
                            if (E.IsReady()) E.Cast(epos);
                        }
                    }

                    if (args.SData.Name.Contains("XenZhaoThrust3"))
                    {
                        if (args.Target.NetworkId == myHero.NetworkId)
                        {
                            if (W.IsReady()) W.Cast();
                        }
                    }
                    if (args.SData.Name.Contains("RengarQ"))
                    {
                        if (args.Target.NetworkId == myHero.NetworkId)
                        {
                            if (E.IsReady()) E.Cast(epos);
                        }
                    }
                    if (args.SData.Name.Contains("RengarPassiveBuffDash"))
                    {
                        if (args.Target.NetworkId == myHero.NetworkId)
                        {
                            if (E.IsReady()) E.Cast(epos);
                        }
                    }
                    if (args.SData.Name.Contains("RengarPassiveBuffDashAADummy"))
                    {
                        if (args.Target.NetworkId == myHero.NetworkId)
                        {
                            if (E.IsReady()) E.Cast(epos);
                        }
                    }
                    if (args.SData.Name.Contains("TwitchEParticle"))
                    {
                        if (args.Target.NetworkId == myHero.NetworkId)
                        {
                            if (E.IsReady()) E.Cast(epos);
                        }
                    }
                    if (args.SData.Name.Contains("FizzPiercingStrike"))
                    {
                        if (args.Target.NetworkId == myHero.NetworkId)
                        {
                            if (E.IsReady()) E.Cast(epos);
                        }
                    }
                    if (args.SData.Name.Contains("HungeringStrike"))
                    {
                        if (args.Target.NetworkId == myHero.NetworkId)
                        {
                            if (E.IsReady()) E.Cast(epos);
                        }
                    }
                    if (args.SData.Name.Contains("YasuoDash"))
                    {
                        if (args.Target.NetworkId == myHero.NetworkId)
                        {
                            if (E.IsReady()) E.Cast(epos);
                        }
                    }
                    if (args.SData.Name.Contains("KatarinaRTrigger"))
                    {
                        if (args.Target.NetworkId == myHero.NetworkId)
                        {
                            if (W.IsReady() && W.IsInRange(sender)) W.Cast();
                            else if (E.IsReady()) E.Cast(epos);
                        }
                    }
                    if (args.SData.Name.Contains("YasuoDash"))
                    {
                        if (args.Target.NetworkId == myHero.NetworkId)
                        {
                            if (E.IsReady()) E.Cast(epos);
                        }
                    }
                    if (args.SData.Name.Contains("KatarinaE"))
                    {
                        if (args.Target.NetworkId == myHero.NetworkId)
                        {
                            if (W.IsReady()) W.Cast();
                        }
                    }
                    if (args.SData.Name.Contains("MonkeyKingQAttack"))
                    {
                        if (args.Target.NetworkId == myHero.NetworkId)
                        {
                            if (E.IsReady()) E.Cast(epos);
                        }
                    }
                    if (args.SData.Name.Contains("MonkeyKingSpinToWin"))
                    {
                        if (args.Target.NetworkId == myHero.NetworkId)
                        {
                            if (E.IsReady()) E.Cast(epos);
                            else if (W.IsReady()) W.Cast();
                        }
                    }
                    if (args.SData.Name.Contains("MonkeyKingQAttack"))
                    {
                        if (args.Target.NetworkId == myHero.NetworkId)
                        {
                            if (E.IsReady()) E.Cast(epos);
                        }
                    }
                    if (args.SData.Name.Contains("MonkeyKingQAttack"))
                    {
                        if (args.Target.NetworkId == myHero.NetworkId)
                        {
                            if (E.IsReady()) E.Cast(epos);
                        }
                    }
                    if (args.SData.Name.Contains("MonkeyKingQAttack"))
                    {
                        if (args.Target.NetworkId == myHero.NetworkId)
                        {
                            if (E.IsReady()) E.Cast(epos);
                        }
                    }
                }
            }
        }

        private static int lastQ;
        private static int lastQDelay;
        private static int QNum = 0;
        private static void OnPlay(Obj_AI_Base sender, GameObjectPlayAnimationEventArgs args)
        {
            if (myHero.IsDead) return;
            if (!sender.IsMe) return;
            int delay = 0;
            switch (args.Animation)
            {
                case "Spell1a":
                    delay = Q1Delay;
                    lastQ = Core.GameTickCount;
                    QNum = 1;
                    break;
                case "Spell1b":
                    delay = Q2Delay;
                    lastQ = Core.GameTickCount;
                    QNum = 2;
                    break;
                case "Spell1c":
                    delay = Q3Delay;
                    lastQ = Core.GameTickCount;
                    QNum = 3;
                    break;
                case "Dance":
                    if (lastQ > Core.GameTickCount - 500)
                    {

                        //Orbwalker.ResetAutoAttack();
                        //Utils.Debug("reset");
                    }

                    break;
            }

            if (delay != 0 && (Orbwalker.ActiveModesFlags != Orbwalker.ActiveModes.None || AlwaysCancel))
            {
                lastQDelay = delay;
                Orbwalker.ResetAutoAttack();
                Core.DelayAction(DanceIfNotAborted, delay - Game.Ping);
                //Utils.Debug("reset"); 
            }


        }

        private static void DanceIfNotAborted()
        {
            Player.DoEmote(Emote.Dance);

        }

        private static bool InWRange(GameObject target)
        {
            if (target == null || !target.IsValid) return false;
            return (myHero.HasBuff("RivenFengShuiEngine"))
            ? 330 >= myHero.Distance(target.Position)
            : 265 >= myHero.Distance(target.Position);

        }

        private static float getComboDamage(Obj_AI_Base enemy)
        {
            if (enemy != null)
            {
                float damage = 0;
                float passivenhan;
                if (myHero.Level >= 18)
                {
                    passivenhan = 0.5f;
                }
                else if (myHero.Level >= 15)
                {
                    passivenhan = 0.45f;
                }
                else if (myHero.Level >= 12)
                {
                    passivenhan = 0.4f;
                }
                else if (myHero.Level >= 9)
                {
                    passivenhan = 0.35f;
                }
                else if (myHero.Level >= 6)
                {
                    passivenhan = 0.3f;
                }
                else if (myHero.Level >= 3)
                {
                    passivenhan = 0.25f;
                }
                else
                {
                    passivenhan = 0.2f;
                }
                if (W.IsReady()) damage = damage + ObjectManager.Player.GetSpellDamage(enemy, SpellSlot.W);
                if (Q.IsReady())
                {
                    var qnhan = 4 - QNum;
                    damage = damage + ObjectManager.Player.GetSpellDamage(enemy, SpellSlot.Q) * qnhan +
                             myHero.GetAutoAttackDamage(enemy) * qnhan * (1 + passivenhan);
                }
                damage = damage + myHero.GetAutoAttackDamage(enemy) * (1 + passivenhan);
                if (R1.IsReady())
                {
                    return damage * 1.2f + ObjectManager.Player.GetSpellDamage(enemy, SpellSlot.R);
                }

                return damage;
            }
            return 0;
        }

        private static double basicdmg(Obj_AI_Base target)
        {
            if (target != null)
            {
                double dmg = 0;
                double passivenhan;
                if (myHero.Level >= 18)
                {
                    passivenhan = 0.5;
                }
                else if (myHero.Level >= 15)
                {
                    passivenhan = 0.45;
                }
                else if (myHero.Level >= 12)
                {
                    passivenhan = 0.4;
                }
                else if (myHero.Level >= 9)
                {
                    passivenhan = 0.35;
                }
                else if (myHero.Level >= 6)
                {
                    passivenhan = 0.3;
                }
                else if (myHero.Level >= 3)
                {
                    passivenhan = 0.25;
                }
                else
                {
                    passivenhan = 0.2;
                }
          
                if (W.IsReady()) dmg = dmg + myHero.GetSpellDamage(target, SpellSlot.W);
                if (Q.IsReady())
                {
                    var qnhan = 4 - QNum;

                    dmg = dmg + ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q) * qnhan + myHero.GetAutoAttackDamage(target) * qnhan * (1 + passivenhan);
                }
                dmg = dmg + myHero.GetAutoAttackDamage(target) * (1 + passivenhan);
                return dmg;
            }
            return 0;
        }


        private static double Rdame(Obj_AI_Base target, double health)
        {
            if (target != null)
            {
                var missinghealth = (target.MaxHealth - health) / target.MaxHealth > 0.75 ? 0.75 : (target.MaxHealth - health) / target.MaxHealth;
                var pluspercent = missinghealth * (8 / 3);
                var rawdmg = new double[] { 80, 120, 160 }[R1.Level - 1] + 0.6 * myHero.FlatPhysicalDamageMod;
                return myHero.CalculateDamageOnUnit(target, DamageType.Physical, (float)(rawdmg * (1 + pluspercent)));
            }
            return 0;
        }

        private static int lastAA;
        private static AIHeroClient ComboTarget;
        private static void AfterAAQLogic(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe)
                return;

            var t = args.Target;

            if (t == null)
            {
                return;
            }

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                if (Q.IsReady())
                {
                    if (t is AIHeroClient)
                        Q.Cast(t.Position);
                }
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
            {
                if (Q.IsReady())
                {
                    if (harassQ)
                        if (t is AIHeroClient)
                            Q.Cast(t.Position);
                }
            }

            /*
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.QuickHarass)
            {
                if (Q.IsReady() && QStack != 2)
                {
                    if (HarassQ)
                    {
                        if (t is Obj_AI_Hero)
                            Q.Cast(t.Position);
                    }
                }
            }
            */

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) || Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
            {
                if (t is Obj_AI_Minion)
                {
                    if (Q.IsReady())
                    {
                        if (LaneQ)
                        {
                            foreach (var minion in EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, myHero.ServerPosition, E.Range))
                            {
                                Q.Cast(minion);
                            }
                        }
                        if (jungleQ)
                        {
                            foreach (var camp in EntityManager.MinionsAndMonsters.GetJungleMonsters(myHero.ServerPosition, E.Range))
                            {
                                Q.Cast(camp);
                            }
                        }
                    }
                }
            }
        }

        private static void HarassLogic()
        {
            if (harassW)
            {
                var t = EntityManager.Heroes.Enemies.Find(x => x.IsValidTarget(W.Range) && !x.HasBuffOfType(BuffType.SpellShield));

                if (t != null)
                    if (W.IsReady())
                        W.Cast();
            }
        }

        private static void BurstLogic()
        {
            var target = TargetSelector.SelectedTarget;
            Orbwalker.ForcedTarget = target;
            Orbwalker.OrbwalkTo(target.ServerPosition);
            if (target != null && target.IsValidTarget(1000))
            {

                if (E.IsReady())
                {
                    Player.CastSpell(SpellSlot.E, target.ServerPosition);
                }
                if (Flash.IsReady())
                {
                    Flash.Cast(target.ServerPosition);
                }

                if (target.IsValidTarget(W.Range))
                {
                    if (R1.IsReady() && AlwaysR && forceR == false)
                    {
                        R1.Cast();
                    }
                    if (W.IsReady())
                    {
                        W.Cast();
                    }
                    Player.IssueOrder(GameObjectOrder.AttackTo, target);


                    if (target.IsValidTarget(Q.Range))
                    {
                        if (R2.IsReady() && forceR)
                        {
                            var prediction = R2.GetPrediction(target);
                            R2.Cast(prediction.CastPosition);
                        }
                        if (Q.IsReady())
                        {
                            Q.Cast(target.ServerPosition);
                        }
                    }
                }

                ForceQ(target);
            }
        }

        public static void ForceQ(AIHeroClient target)
        {
            if (!target.IsValidTarget()) return;

            if (Player.Instance.Distance(target) < 270)
            {
                forceQ2 = true;
            }
            else
            {
                ForceQ3(target);
            }
        }

        public static AIHeroClient Qtarget;
        public static void ForceQ3(AIHeroClient target)
        {
            if (!target.IsValidTarget()) return;

            Qtarget = target;
            if (Player.Instance.Distance(target) > 270)
            {
                forceQ2 = true;
            }
        }

    

        private static void LaneClearLogic()
        {
            if (LaneW)
            {
                if (W.IsReady())
                {
                    var WMinions = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, myHero.ServerPosition, W.Range).ToList();

                    if (WMinions != null)
                        if (WMinions.FirstOrDefault().IsValidTarget(W.Range))
                            if (WMinions.Count >= 3)
                                W.Cast();
                }
            }
        }

        private static void FleeLogic()
        {
            var enemy =
                EntityManager.Heroes.Enemies.Where(
                    hero =>
                        hero.IsValidTarget(WRange) && W.IsReady());
            var x = myHero.Position.Extend(Game.CursorPos, 300);
            if (W.IsReady() && enemy.Any()) W.Cast();
            if (Q.IsReady() && !myHero.IsDashing()) Player.CastSpell(SpellSlot.Q, Game.CursorPos);
            if (E.IsReady() && !myHero.IsDashing()) Player.CastSpell(SpellSlot.E, x.To3D());
        }

       private static void ForceSkill()
        {
            if (QTarget == null || !QTarget.IsValidTarget()) return;
            if (forceR && R1.Name == IsFirstR)
            {
                //Chat.Print("trying to use R");
                Player.CastSpell(SpellSlot.R);
                return;
            }
            if (forceQ && QTarget != null && QTarget.IsValidTarget(E.Range + myHero.BoundingRadius + 70) && Q.IsReady())
                Player.CastSpell(SpellSlot.Q, ((Obj_AI_Base)QTarget).ServerPosition);
            if (forceW) W.Cast();

            if (forceR2 && R2.Name == IsSecondR)
            {
                var target = TargetSelector.SelectedTarget;

                if (target == null || !target.IsValidTarget()) target = TargetSelector.GetTarget(450 + myHero.AttackRange + 70, DamageType.Physical);
                if (target == null || !target.IsValidTarget()) return;
                R2.Cast(target);
            }
        }

        private static void Killsteal()
        {
            if (KillStealW && W.IsReady())
            {
                var targets = EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget(R2.Range) && !x.IsZombie);
                foreach (var target in targets)
                {
                    if (target.Health < myHero.GetSpellDamage(target, SpellSlot.W) && InWRange(target))
                        W.Cast();
                }
            }
            if (KillStealR && R2.IsReady() && R2.Name == IsSecondR)
            {
                var targets = EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget(R2.Range) && !x.IsZombie);
                foreach (var target in targets)
                {
                    if (target.Health < Rdame(target, target.Health) &&
                        (!target.HasBuff("kindrednodeathbuff") && !target.HasBuff("Undying Rage") &&
                         !target.HasBuff("JudicatorIntervention")))
                        R2.Cast(target.Position);
                }
            }
        }
        private static double totaldame(Obj_AI_Base target)
        {
            if (target != null)
            {
                float dmg = 0;
                float passivenhan;
                if (myHero.Level >= 18)
                {
                    passivenhan = 0.5f;
                }
                else if (myHero.Level >= 15)
                {
                    passivenhan = 0.45f;
                }
                else if (myHero.Level >= 12)
                {
                    passivenhan = 0.4f;
                }
                else if (myHero.Level >= 9)
                {
                    passivenhan = 0.35f;
                }
                else if (myHero.Level >= 6)
                {
                    passivenhan = 0.3f;
                }
                else if (myHero.Level >= 3)
                {
                    passivenhan = 0.25f;
                }
                else
                {
                    passivenhan = 0.2f;
                }
                if (W.IsReady()) dmg = dmg + myHero.GetSpellDamage(target, SpellSlot.W);
                if (Q.IsReady())
                {
                    var qnhan = 4 - QNum;
                    dmg = dmg + ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q) * qnhan + myHero.GetAutoAttackDamage(target) * qnhan * (1 + passivenhan);
                }
                dmg = dmg + myHero.GetAutoAttackDamage(target) * (1 + passivenhan);
                if (R1.IsReady())
                {
                    var rdmg = Rdame(target, target.Health - dmg * 1.2f);
                    return dmg * 1.2 + rdmg;
                }
                return dmg;
            }
            return 0;
        }

        private static void JungleClearLogic()
        {
            var Mob = EntityManager.MinionsAndMonsters.GetJungleMonsters(myHero.ServerPosition, E.Range).OrderBy(x => x.MaxHealth).ToList();

            if (jungleW)
            {
                if (Mob != null)
                    if (Mob.FirstOrDefault().IsValidTarget(W.Range))
                        W.Cast();
            }
        }

        private static void CastYoumoo()
        {
            var youmu = ObjectManager.Player.InventoryItems.FirstOrDefault(it => it.Id == ItemId.Youmuus_Ghostblade);

            if (youmu != null && youmu.CanUseItem()) youmu.Cast();
        }

        private static void JungleClearELogic(AttackableUnit target, EventArgs args)
        {
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) || Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
            {
                if (target is Obj_AI_Minion)
                {
                    if (jungleE)
                    {
                        var Mob = EntityManager.MinionsAndMonsters.GetJungleMonsters(myHero.ServerPosition, E.Range).OrderBy(x => x.MaxHealth).ToList();

                        if (Mob.FirstOrDefault().IsValidTarget(E.Range))
                        {
                            if (Mob.FirstOrDefault().HasBuffOfType(BuffType.Stun) && !W.IsReady())
                            {
                                E.Cast(Game.CursorPos);
                            }
                            else if (!Mob.FirstOrDefault().HasBuffOfType(BuffType.Stun))
                            {
                                E.Cast(Game.CursorPos);
                            }
                        }
                    }
                }
            }
        }

        private static void QuickHarassLogic()
        {
            var t = TargetSelector.GetTarget(E.Range, DamageType.Physical);

            if (t != null && t.IsValidTarget())
            {
                if (_qStack == 2)
                {
                    if (E.IsReady())
                    {
                        E.Cast(myHero.ServerPosition + (myHero.ServerPosition - t.ServerPosition).Normalized() * E.Range);
                    }

                    if (!E.IsReady())
                    {
                        Q.Cast(myHero.ServerPosition + (myHero.ServerPosition - t.ServerPosition).Normalized() * E.Range);
                    }
                }

                if (W.IsReady())
                {
                    if (t.IsValidTarget(W.Range) && _qStack == 1)
                    {
                        W.Cast();
                    }
                }

                if (Q.IsReady())
                {
                    if (_qStack == 0)
                    {
                        if (t.IsValidTarget(myHero.AttackRange + myHero.BoundingRadius + 150))
                        {
                            Q.Cast(t.Position);
                        }
                    }
                }
            }
        }


        private static void KillStealLogic()
        {
            foreach (var e in EntityManager.Heroes.Enemies.Where(e => !e.IsZombie && !e.HasBuff("KindredrNoDeathBuff") && !e.HasBuff("Undying Rage") && !e.HasBuff("JudicatorIntervention") && e.IsValidTarget()))
            {
                if (Q.IsReady() && KillStealQ)
                {
                    if (myHero.HasBuff("RivenFengShuiEngine"))
                    {
                        if (e.Distance(myHero.ServerPosition) < myHero.AttackRange + myHero.BoundingRadius + 50 && myHero.GetSpellDamage(e, SpellSlot.Q) > e.Health + e.HPRegenRate)
                            Q.Cast(e.Position);
                    }
                    else if (!myHero.HasBuff("RivenFengShuiEngine"))
                    {
                        if (e.Distance(myHero.ServerPosition) < myHero.AttackRange + myHero.BoundingRadius && myHero.GetSpellDamage(e, SpellSlot.Q) > e.Health + e.HPRegenRate)
                            Q.Cast(e.Position);
                    }
                }

                if (W.IsReady() && KillStealW)
                {
                    if (e.IsValidTarget(W.Range) && myHero.GetSpellDamage(e, SpellSlot.W) > e.Health + e.HPRegenRate)
                    {
                        W.Cast();
                    }
                }

                if (R1.IsReady() && KillStealR)
                {
                    if (myHero.HasBuff("RivenWindScarReady"))
                    {
                        if (E.IsReady() && KillStealE)
                        {
                            if (myHero.ServerPosition.CountEnemiesInRange(R2.Range + E.Range) < 3 && myHero.HealthPercent > 50)
                            {
                                if (Rdame(e, e.Health) > e.Health + e.HPRegenRate && e.IsValidTarget(R2.Range + E.Range - 100))
                                {
                                    R1.Cast();
                                    E.Cast(e.Position);
                                    Core.DelayAction(() => { R2.Cast(e); }, 350);
                                }
                            }
                        }
                        else
                        {
                            if (Rdame(e, e.Health) > e.Health + e.HPRegenRate && e.IsValidTarget(R2.Range - 50))
                            {
                                R1.Cast();
                                R2.Cast(e);
                            }
                        }
                    }
                }
            }
        }


        private static void Drawing_OnDraw(EventArgs args)
        {
            //temp
            if (myHero.IsDead)
                return;
            var heropos = Drawing.WorldToScreen(ObjectManager.Player.Position);


            /*if (QStack != 1 && DrawTimer1)
            {
                Timer.text = ("Q Expiry =>  " + ((double) (LastQ - Environment.TickCount + 3800)/1000).ToString("0.0") +
                              "S");
                Timer.OnEndScene();
            }
            if (Player.HasBuff("RivenFengShuiEngine") && DrawTimer2)
            {
                Timer2.text = ("R Expiry =>  " +
                               (((double) LastR - Environment.TickCount + 15000)/1000).ToString("0.0") + "S");
                Timer2.OnEndScene();
            }*/
            var green = Color.LimeGreen;
            var red = Color.IndianRed;
            if (DrawCB)
                Circle.Draw(E.IsReady() ? green : red, 250 + myHero.AttackRange + 70, ObjectManager.Player.Position);
          //    if (DrawBT && Flash.Range)
           //       Circle.Draw(R1.IsReady() && Flash.IsReady ? green : red, 800, ObjectManager.Player.Position);
            if (DrawFH)
                Circle.Draw(E.IsReady() && Q.IsReady() ? green : red, 450 + myHero.AttackRange + 70,
                    ObjectManager.Player.Position);       
            if (DrawAlwaysR)
            {
                Drawing.DrawText(heropos.X - 40, heropos.Y + 20, System.Drawing.Color.WhiteSmoke, "ForceR");
                Drawing.DrawText(heropos.X + 10, heropos.Y + 20,
                    AlwaysR ? System.Drawing.Color.LimeGreen : System.Drawing.Color.Red,
                    AlwaysR ? "On" : "Off");
            }

            //Drawing.DrawText(heropos.X - 40, heropos.Y + 43, System.Drawing.Color.DodgerBlue, "Can AA:");
            //Drawing.DrawText(heropos.X + 50, heropos.Y + 43,
            //        Orbwalker.CanAutoAttack ? System.Drawing.Color.LimeGreen : System.Drawing.Color.Red,
            //        Orbwalker.CanAutoAttack ? "true" : "false");
        }



        private static float Cooldown(SpellDataInst spell)
        {
            return Player.Spells[0].CooldownExpires - Game.Time;
        }

        private static double RDamage(Obj_AI_Base target)
        {

            if (target != null && R1.IsReady())
            {
                float missinghealth = (target.MaxHealth - target.Health) / target.MaxHealth > 0.75f
                    ? 0.75f
                    : (target.MaxHealth - target.Health) / target.MaxHealth;
                float pluspercent = missinghealth * (2.666667F); // 8/3
                float rawdmg = new float[] { 80, 120, 160 }[R1.Level - 1] + 0.6f * myHero.FlatPhysicalDamageMod;
                return Player.Instance.CalculateDamageOnUnit(target, DamageType.Physical, rawdmg * (1 + pluspercent));
            }
            return 0;
        }
        private static int TickLimiter = 1;
        private static int LastGameTick = 0;
        private static void OnTick(EventArgs args)
        {

            if (lastQ + 3650 < Core.GameTickCount)
                QNum = 0;
            KillStealLogic();
            AutoUseW();
            if (doBurst) BurstLogic();
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo)) ComboLogic();
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass)) QuickHarassLogic();
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Flee))
            {
                FleeLogic();
                CastYoumoo();
            }

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) || Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
            {
                LaneClearLogic();
                JungleClearLogic();
            }

        }
    }
}
