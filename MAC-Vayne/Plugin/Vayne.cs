using System;
using LeagueSharp;
using LeagueSharp.Common;
using MAC_Vayne.Util;
using SharpDX;
using Color = System.Drawing.Color;

namespace MAC_Vayne.Plugin
{
    static class Vayne
    {

        #region Global Variables

        /*
         Config
         */

        public static string G_version = "1.5.1";
        public static string G_charname = _Player.ChampionName;

        /*
         Spells
         */
        public static Spell.Ranged Q;
        public static Spell.Targeted E;
        public static Spell.Active R;

        /*
         Menus
         */

        public static Menu Menu,
            ComboMenu,
            LaneClearMenu,
            CondemnMenu,
            KSMenu,
            DrawMenu;

        /*
         Misc
         */

        public static AIHeroClient _target;

        #endregion

        #region Initialization

        public static AIHeroClient _Player
        {
            get { return ObjectManager.Player; }
        }

        public static void Init()
        {
            Bootstrap.Init(null);
            
            InitVariables();

            Orbwalker.OnPostAttack += OnAfterAttack;
            Orbwalker.OnPreAttack += OnBeforeAttack;
            Gapcloser.OnGapcloser += OnGapCloser;
            Interrupter.OnInterruptableSpell += OnPossibleToInterrupt;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpell;

            Game.OnUpdate += OnGameUpdate;
            Drawing.OnDraw += OnDraw;
        }

        public static void InitVariables()
        {
            Q = new Spell.Skillshot(SpellSlot.Q, 325, SkillShotType.Linear);
            E = new Spell.Targeted(SpellSlot.E, 550);
            R = new Spell.Active(SpellSlot.R);
            InitMenu();
        }

        public static void InitMenu()
        {
            Menu = MainMenu.AddMenu("MAC - " + G_charname, "vania");

            Menu.AddGroupLabel("MAC - " + G_charname);
            Menu.AddLabel("Version: " + G_version);
            Menu.AddSeparator();
            Menu.AddLabel("By Mr Articuno");

            /*Brain.Common.Selector.Init(Menu);*/

            DrawMenu = Menu.AddSubMenu("Draw - " + G_charname, "vaniaDraw");
            DrawMenu.AddGroupLabel("Draw");
            DrawMenu.Add("drawDisable", new CheckBox("Turn off all drawings", false));
            DrawMenu.Add("drawNameLine", new CheckBox("Show names on line", true));
            DrawMenu.Add("drawAARange", new CheckBox("Draw Auto Attack Range", true));
            DrawMenu.Add("drawQ", new CheckBox("Draw Q Range", true));
            DrawMenu.Add("drawE", new CheckBox("Draw E Range", true));
            DrawMenu.Add("drawTumblePos", new CheckBox("Draw Tumble Pos", true));
            DrawMenu.Add("wallTumble", new KeyBind("Wall Tumble", false, KeyBind.BindTypes.HoldActive, 'W'));
            DrawMenu.Add("drawCondemnPos", new CheckBox("Draw Condemn Position", true));

            ComboMenu = Menu.AddSubMenu("Combo - " + G_charname, "vaniaCombo");
            ComboMenu.AddGroupLabel("Combo");
            ComboMenu.Add("comboQ", new CheckBox("Allow Q usage in combo", true));
            ComboMenu.Add("comboE", new CheckBox("Allow E usage in combo", true));
            ComboMenu.Add("comboR", new CheckBox("Allow R usage in combo", true));
            ComboMenu.AddGroupLabel("Q Settings");
            ComboMenu.AddLabel("Q Direction: Checked - Target, Unchecked Cursor");
            ComboMenu.Add("qsQDirection", new CheckBox("Q Direction", false));
            ComboMenu.AddLabel("Q Usage: Checked - Before Auto Attack, Unchecked After Auto Attack");
            ComboMenu.Add("qsQUsage", new CheckBox("Q Usage", false));
            ComboMenu.Add("qsQOutAA", new CheckBox("Q if out of AA range", true));
            ComboMenu.AddGroupLabel("R Settings");
            ComboMenu.Add("rsMinEnemiesForR", new Slider("Min Enemies for cast R: ", 2, 1, 5));
            ComboMenu.AddGroupLabel("Misc");
            /*ComboMenu.Add("advTargetSelector", new CheckBox("Use Advanced Target Selector", false));*/
            ComboMenu.Add("forceSilverBolt", new CheckBox("Force Attack 2 Stacked Target", false));
            ComboMenu.Add("checkKillabeEnemyPassive", new CheckBox("Double Check if enemy is killabe", true));

            CondemnMenu = Menu.AddSubMenu("Condemn - " + G_charname, "vaniaCondemn");
            CondemnMenu.Add("interruptDangerousSpells", new CheckBox("Interrupt Dangerous Spells", true));
            CondemnMenu.Add("antiGapCloser", new CheckBox("Anti Gap Closer", true));
            CondemnMenu.Add("fastCondemn",
                new KeyBind("Fast Condemn HotKey", false, KeyBind.BindTypes.PressToggle, 'W'));
            CondemnMenu.AddGroupLabel("Auto Condemn");
            foreach (var enemy in ObjectManager.Get<AIHeroClient>().Where(a => a.IsEnemy))
            {
                CondemnMenu.Add("dnCondemn" + enemy.ChampionName.ToLower(), new CheckBox("Don't Condemn " + enemy.ChampionName, false));
            }
            CondemnMenu.AddGroupLabel("Priority Condemn");
            foreach (var enemy in ObjectManager.Get<AIHeroClient>().Where(a => a.IsEnemy))
            {
                CondemnMenu.Add("priorityCondemn" + enemy.ChampionName.ToLower(), new Slider(enemy.ChampionName + " Priority", 1, 1, 5));
            }
            CondemnMenu.Add("condenmErrorMargin", new Slider("Subtract Condemn Push by: ", 20, 0, 100));

            KSMenu = Menu.AddSubMenu("KS - " + G_charname, "vaniaKillSteal");
            KSMenu.AddGroupLabel("Kill Steal");
            KSMenu.Add("ksQ", new CheckBox("Use Q if killable", false));
            KSMenu.Add("ksE", new CheckBox("Use E if killable", false));
        }

        #endregion

        public static void OnDraw(EventArgs args)
        {
            if (Misc.isChecked(DrawMenu, "drawDisable"))
                return;

            if (Misc.isChecked(DrawMenu, "drawAARange"))
            {
                new Circle() { Color = Color.Cyan, Radius = _Player.GetAutoAttackRange(), BorderWidth = 2f }.Draw(_Player.Position);
                if (Misc.isChecked(DrawMenu, "drawNameLine"))
                    Drawing.DrawText(Drawing.WorldToScreen(_Player.Position) - new Vector2(_Player.GetAutoAttackRange() - 250, 0), Color.Cyan, "Auto Attack", 15);
            }

            if (Misc.isChecked(DrawMenu, "drawQ") && Q.IsReady())
            {
                new Circle() { Color = Color.White, Radius = Q.Range, BorderWidth = 2f }.Draw(_Player.Position);
                if (Misc.isChecked(DrawMenu, "drawNameLine"))
                    Drawing.DrawText(Drawing.WorldToScreen(_Player.Position) - new Vector2(Q.Range - 100, 0), Color.White, "Q Range", 15);
            }

            if (Misc.isChecked(DrawMenu, "drawE") && E.IsReady())
            {

                new Circle() { Color = Color.White, Radius = E.Range, BorderWidth = 2f }.Draw(_Player.Position);
                if (Misc.isChecked(DrawMenu, "drawNameLine"))
                    Drawing.DrawText(Drawing.WorldToScreen(_Player.Position) - new Vector2(E.Range - 290, -50), Color.White, "E Range", 15);
            }

            if (Misc.isChecked(DrawMenu, "drawCondemnPos") && E.IsReady())
            {
                foreach (var enemy in ObjectManager.Get<AIHeroClient>().Where(a => a.IsEnemy).Where(a => !a.IsDead).Where(a => _Player.Distance(a) <= E.Range))
                {
                    if (Misc.isChecked(CondemnMenu, "dnCondemn" + enemy.ChampionName.ToLower()))
                        return;

                    var condemnPos = _Player.Position.Extend(enemy.Position, _Player.Distance(enemy) + 470 - Misc.getSliderValue(CondemnMenu, "condenmErrorMargin"));

                    var realStart = Drawing.WorldToScreen(enemy.Position);
                    var realEnd = Drawing.WorldToScreen(condemnPos.To3D());

                    Drawing.DrawLine(realStart, realEnd, 2f, Color.Red);
                    new Circle() { Color = Color.Red, Radius = 60, BorderWidth = 2f }.Draw(condemnPos.To3D());
                }
            }

            if(Misc.isChecked(DrawMenu, "drawTumblePos"))
            {
                Misc.Drawing_OnDraw();
            }

        }

        public static void OnAfterAttack(AttackableUnit target, EventArgs args)
        {
            if (target != null && (!target.IsValid || target.IsDead))
                return;

            var orbwalkermode = Orbwalker.ActiveModesFlags;

            if (orbwalkermode == Orbwalker.ActiveModes.Combo)
            {
                if (Misc.isChecked(ComboMenu, "comboQ") && Q.IsReady() && !Misc.isChecked(ComboMenu, "qsQUsage") && _Player.Distance(_target.Position) < (_Player.GetAutoAttackRange() + Q.Range))
                {
                    if (Misc.isChecked(ComboMenu, "qsQDirection"))
                    {
                        if (target != null) Q.Cast(target.Position);
                    }
                    else
                    {
                        Player.CastSpell(SpellSlot.Q, Game.CursorPos);
                    }
                }
            }

        }

        public static void OnBeforeAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            if (target != null && (!target.IsValid || target.IsDead))
                return;

            var orbwalkermode = Orbwalker.ActiveModesFlags;

            if (orbwalkermode == Orbwalker.ActiveModes.Combo)
            {
                if (Misc.isChecked(ComboMenu, "comboQ") && Q.IsReady() && Misc.isChecked(ComboMenu, "qsQUsage") && _Player.Distance(_target.Position) < (_Player.GetAutoAttackRange() + Q.Range))
                {
                    if (Misc.isChecked(ComboMenu, "qsQDirection"))
                    {
                        if (target != null) Q.Cast(target.Position);
                    }
                    else
                    {
                        Player.CastSpell(SpellSlot.Q, Game.CursorPos);
                    }
                }
            }
        }

        public static void OnProcessSpell(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe) return;
            if (args.SData.Name.ToLower().Contains("vaynetumble"))
            {
                Core.DelayAction(Orbwalker.ResetAutoAttack, 250);
            }
        }

        public static void OnGapCloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs e)
        {
            if (sender == null || sender.IsAlly || !Misc.isChecked(CondemnMenu, "antiGapCloser")) return;

            if ((sender.IsAttackingPlayer || e.End.Distance(_Player) <= 70))
            {
                E.Cast(sender);
            }
        }

        public static void OnPossibleToInterrupt(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs interruptableSpellEventArgs)
        {
            if (sender == null || sender.IsAlly || !Misc.isChecked(CondemnMenu, "interruptDangerousSpells")) return;

            if (interruptableSpellEventArgs.DangerLevel == DangerLevel.High && E.IsInRange(sender))
            {
                E.Cast(sender);
            }
        }

        public static void OnLasthit()
        {
            //  OnLasthit
        }

        public static void OnLaneClear()
        {

        }

        public static void OnHarass()
        {
            //  OnHarass
        }

        public static void OnCombo()
        {
            if (_target == null || !_target.IsValid)
                return;

            if (Misc.isChecked(ComboMenu, "forceSilverBolt"))
            {
                foreach (var enemy in ObjectManager.Get<AIHeroClient>().Where(t => t.IsEnemy).Where(t => _Player.GetAutoAttackRange() >= t.Distance(_Player)).Where(t => t.IsValidTarget()))
                {
                    if (enemy.PossibleDamage() >= enemy.Health)
                    {
                        _target = enemy;
                        Orbwalker.ForcedTarget = enemy;
                        break;
                    }
                    else
                    {
                        if (enemy.Has2WStacks())
                        {
                            _target = enemy;
                            Orbwalker.ForcedTarget = enemy;
                            break;
                        }
                    }
                }
            }

            if (Misc.isChecked(ComboMenu, "forceSilverBolt"))
            {
                foreach (var enemy in ObjectManager.Get<AIHeroClient>().Where(t => t.IsEnemy).Where(t => _Player.GetAutoAttackRange() >= t.Distance(_Player)).Where(t => t.IsValidTarget()))
                {
                    if (enemy.PossibleDamage() >= enemy.Health)
                    {
                        _target = enemy;
                        Orbwalker.ForcedTarget = enemy;
                        break;
                    }
                    else
                    {
                        if (enemy.Has2WStacks())
                        {
                            _target = enemy;
                            Orbwalker.ForcedTarget = enemy;
                            break;
                        }
                    }
                }
            }

            if (Misc.isChecked(ComboMenu, "comboR") && R.IsReady())
            {
                if (_Player.CountEnemiesInRange(_Player.GetAutoAttackRange()) >= Misc.getSliderValue(ComboMenu, "rsMinEnemiesForR"))
                {
                    R.Cast();
                }
            }
            if (Misc.isChecked(ComboMenu, "comboQ") && Q.IsReady())
            {
                if (Misc.isChecked(ComboMenu, "qsQOutAA") && _Player.Distance(_target.Position) < (_Player.GetAutoAttackRange() + Q.Range))
                {
                    if (Misc.isChecked(ComboMenu, "qsQDirection"))
                    {
                        Q.Cast(_target.Position);
                    }
                    else
                    {
                        Player.CastSpell(SpellSlot.Q, Game.CursorPos);
                    }
                }
            }

            if (Misc.isChecked(ComboMenu, "comboE") && E.IsReady())
            {
                AIHeroClient priorityTarget = null;
                foreach (var enemy in ObjectManager.Get<AIHeroClient>().Where(a => a.IsEnemy).Where(a => !a.IsDead).Where(a => E.IsInRange(a)))
                {
                    if (priorityTarget == null)
                    {
                        priorityTarget = enemy;
                    }
                    else
                    {
                        if (Misc.getSliderValue(CondemnMenu, "priorityCondemn" + enemy.ChampionName.ToLower()) > Misc.getSliderValue(CondemnMenu, "priorityCondemn" + priorityTarget.ChampionName.ToLower()))
                        {
                            priorityTarget = enemy;
                        }
                    }

                    if (!Misc.IsCondemnable(priorityTarget))
                        return;
                }

                if (priorityTarget != null && priorityTarget.IsValid && Misc.IsCondemnable(priorityTarget))
                {
                    E.Cast(priorityTarget);
                }
            }
        }

        public static void OnGameUpdate(EventArgs args)
        {
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
            {
                Orbwalker.ForcedTarget = null;
                OnLaneClear();
            }

            switch (Orbwalker.ActiveModesFlags)
            {
                case Orbwalker.ActiveModes.Combo:
                    /*if (Misc.isChecked(ComboMenu, "advTargetSelector"))
                    {
                        _target = Brain.Common.Selector.GetTarget(1100, DamageType.Physical, true);
                    }
                    else
                    {*/
                        _target = TargetSelector.GetTarget(1100, DamageType.Physical);
                    //}
                    OnCombo();
                    break;
                case Orbwalker.ActiveModes.LastHit:
                    Orbwalker.ForcedTarget = null;
                    OnLasthit();
                    break;
                case Orbwalker.ActiveModes.Harass:
                    Orbwalker.ForcedTarget = null;
                    OnHarass();
                    break;
            }

            if (Misc.isKeyBindActive(DrawMenu, "wallTumble"))
            {
                Misc.WallTumble();
            }
            else
            {
                Orbwalker.DisableMovement = false;
            }
        }
    }
}
