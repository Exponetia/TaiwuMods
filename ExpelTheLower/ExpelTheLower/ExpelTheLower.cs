using GameData.Common;
using GameData.Domains;
using GameData.Domains.World;
using HarmonyLib;
using System.Data;
using TaiwuModdingLib.Core.Plugin;

namespace ExpelTheLower
{
    [PluginConfig("ExpelTheLower", "Exponetia", "1.0")]
    public class ExpelTheLower : TaiwuRemakePlugin
    {
        private Harmony harmony;

        // Is the expel process allowed ?
        private static bool doExpel;
 
        public override void OnModSettingUpdate()
        {
            DomainManager.Mod.GetSetting(base.ModIdStr, "doExpel", ref ExpelTheLower.doExpel);
            DomainManager.Mod.GetSetting(base.ModIdStr, "maximalAge", ref Evaluation.maximalAge);
            DomainManager.Mod.GetSetting(base.ModIdStr, "minimalCharm", ref Evaluation.minimalCharm);
            DomainManager.Mod.GetSetting(base.ModIdStr, "forbiddenBehavior", ref Evaluation.forbiddenBehavior);

            DomainManager.Mod.GetSetting(base.ModIdStr, "minimalLifeSkillAttainment", ref Evaluation.minimalLifeSkillAttainment);
            DomainManager.Mod.GetSetting(base.ModIdStr, "minimalLifeSkillQualification", ref Evaluation.minimalLifeSkillQualification);
            DomainManager.Mod.GetSetting(base.ModIdStr, "minimalCombatSkillAttainment", ref Evaluation.minimalCombatSkillAttainment);
            DomainManager.Mod.GetSetting(base.ModIdStr, "minimalCombatSkillQualification", ref Evaluation.minimalCombatSkillQualification);

            DomainManager.Mod.GetSetting(base.ModIdStr, "DebugInfo", ref Evaluation.debugInfo);
        }

        public override void Initialize()
        {
            harmony = Harmony.CreateAndPatchAll(typeof(ExpelTheLower), null);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(WorldDomain), "AdvanceMonth")]
        public static void WorldDomain_AdvanceMonth_Prefix(DataContext context)
        {
            if (doExpel && Evaluation.AreParametersSet()) 
            {
                Evaluation.GetRidTheLower(context);
            }
        }

        

        public override void Dispose()
        {
            if (harmony != null)
            {
                harmony.UnpatchSelf();
            }
        }
    }
}
