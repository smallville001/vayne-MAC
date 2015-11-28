using System.Collections.Generic;
using System.Linq;
using System;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;
using System;
using MAC_Vayne.Plugin;

namespace MAC_Vayne.Util
{
    static class Misc
    {
        public static AIHeroClient _Player
        {
            get { return ObjectManager.Player; }
        }

        public static bool isChecked(Menu obj, String value)
        {
            return obj[value].Cast<CheckBox>().CurrentValue;
        }

        public static int getSliderValue(Menu obj, String value)
        {
            return obj[value].Cast<Slider>().CurrentValue;
        }

        public static bool isKeyBindActive(Menu obj, String value)
        {
            return obj[value].Cast<KeyBind>().CurrentValue;
        }

        public static bool Has2WStacks(this AIHeroClient target)
        {
            return target.Buffs.Any(bu => bu.Name == "vaynesilvereddebuff");
        }

        public static double PossibleDamage(this AIHeroClient target)
        {
            var damage = 0d;
            var targetMaxHealth = target.MaxHealth;

            var silverBoltDmg = (new float[] {0, 20, 30, 40, 50, 60}[Player.Instance.Spellbook.GetSpell(SpellSlot.W).Level] + new float[] { 0, targetMaxHealth / 4, targetMaxHealth / 5, targetMaxHealth / 6, targetMaxHealth / 7, targetMaxHealth / 8 }[Player.Instance.Spellbook.GetSpell(SpellSlot.W).Level]);

            if (Orbwalker.CanAutoAttack) damage += _Player.GetAutoAttackDamage(target, true);

            if (target.Has2WStacks()) damage += silverBoltDmg;

            return damage;
        }

        public static void Drawing_OnDraw()
        {
            if (Game.MapId != GameMapId.SummonersRift) return;
            Vector2 drakeWallQPos = new Vector2(12050, 4827);
            Vector2 midWallQPos = new Vector2(6962, 8952);
            if (drakeWallQPos.Distance(_Player) < 3000)
                new Circle() { Color = _Player.Distance(drakeWallQPos) <= 100 ? Color.DodgerBlue : Color.White, Radius = 100 }.Draw(drakeWallQPos.To3D());
            if (midWallQPos.Distance(_Player) < 3000)
                new Circle() { Color = _Player.Distance(midWallQPos) <= 100 ? Color.DodgerBlue : Color.White, Radius = 100 }.Draw(midWallQPos.To3D());

        }

        public static void WallTumble()
        {
            if (Game.MapId != GameMapId.SummonersRift) return;
            if (!Vayne.Q.IsReady())
            {
                Orbwalker.DisableMovement = false;
                return;
            }
            Orbwalker.DisableMovement = true;

            Vector2 drakeWallQPos = new Vector2(11514, 4462);
            Vector2 midWallQPos = new Vector2(6667, 8794);

            var selectedPos = drakeWallQPos.Distance(_Player) < midWallQPos.Distance(_Player) ? drakeWallQPos : midWallQPos;
            var walkPos = drakeWallQPos.Distance(_Player) < midWallQPos.Distance(_Player)
                ? new Vector2(12050, 4827)
                : new Vector2(6962, 8952);
            if (_Player.Distance(walkPos) < 200 && _Player.Distance(walkPos) > 60)
            {
                Player.IssueOrder(GameObjectOrder.MoveTo, walkPos.To3D());
            }
            else if (_Player.Distance(walkPos) <= 50)
            {
                Player.CastSpell(SpellSlot.Q, selectedPos.To3D());
            }
        }

        public static bool IsCondemnable(AIHeroClient target)
        {
            if (isChecked(Vayne.CondemnMenu, "dnCondemn" + target.ChampionName.ToLower()))
            {
                return false;
            }

            if (target.HasBuffOfType(BuffType.SpellImmunity) || target.HasBuffOfType(BuffType.SpellShield) || _Player.IsDashing()) return false;

            var predPos = Prediction.Position.PredictUnitPosition(target, 500);

            var position = Vayne._Player.Position.Extend(target.Position, Vayne._Player.Distance(target) - getSliderValue(Vayne.CondemnMenu, "condenmErrorMargin")).To3D();
            var predictPos = Vayne._Player.Position.Extend(predPos, Vayne._Player.Distance(predPos) - getSliderValue(Vayne.CondemnMenu, "condenmErrorMargin")).To3D();
            for (int i = 0; i < 470 - getSliderValue(Vayne.CondemnMenu, "condenmErrorMargin"); i += 10)
            {
                var cPos = _Player.Position.Extend(position, _Player.Distance(position) + i).To3D();
                var cPredPos = _Player.Position.Extend(predictPos, _Player.Distance(predictPos) + i).To3D();

                if ((cPredPos.ToNavMeshCell().CollFlags.HasFlag(CollisionFlags.Wall) || cPredPos.ToNavMeshCell().CollFlags.HasFlag(CollisionFlags.Building)) && (cPos.ToNavMeshCell().CollFlags.HasFlag(CollisionFlags.Wall) || cPos.ToNavMeshCell().CollFlags.HasFlag(CollisionFlags.Building)))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
