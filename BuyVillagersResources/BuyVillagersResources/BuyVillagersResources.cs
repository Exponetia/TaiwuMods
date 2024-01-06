using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaiwuModdingLib;
using TaiwuModdingLib.Core.Plugin;
using HarmonyLib;
using GameData.Domains;
using GameData.Domains.World;
using GameData.Common;

namespace BuyVillagersResources
{
    [PluginConfig("BuyVillagersResources", "Exponetia", "1.0")]
    public class BuyVillagersResources : TaiwuRemakePlugin
    {
        private Harmony harmony;

        // Is resource commerce allowed.
        // When true, villagers sell his/her/iel resources
        private static bool buyResources;

        public override void OnModSettingUpdate()
        {
            DomainManager.Mod.GetSetting(base.ModIdStr, "buyResources", ref BuyVillagersResources.buyResources);
            DomainManager.Mod.GetSetting(base.ModIdStr, "minimumPlants", ref Resource.minimumPlants);
            DomainManager.Mod.GetSetting(base.ModIdStr, "TaiwuBudgetRate", ref Resource.taiwuBudgetRate);
            DomainManager.Mod.GetSetting(base.ModIdStr, "TaiwuRenownRate", ref Resource.taiwuRenownRate);
            DomainManager.Mod.GetSetting(base.ModIdStr, "DebugInfo", ref Resource.debugInfo);
        }

        public override void Initialize()
        {
            harmony = Harmony.CreateAndPatchAll(typeof(BuyVillagersResources), null);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(WorldDomain), "AdvanceMonth")]
        public static void WorldDomain_AdvanceMonth_Prefix(DataContext context)
        {
            if (buyResources)
            {
                Resource.DoCommerce(context);
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