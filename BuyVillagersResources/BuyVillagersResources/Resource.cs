using Config;
using GameData.Common;
using GameData.Domains;
using GameData.Domains.Character;
using GameData.Domains.Combat;
using GameData.Domains.Global;
using GameData.Domains.Item;
using GameData.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BuyVillagersResources
{
    public class Resource
    {

        // Taiwu's money proportion use to buy resources
        public static int taiwuBudgetRate;

        // Taiwu's renown proportion use to buy resources
        public static int taiwuRenownRate;

        // The Villager plants lower bound
        public static int minimumPlants;

        // Write debug info or not.
        public static bool debugInfo;

        public static void DoCommerce(DataContext context)
        {
            if (debugInfo) { AdaptableLog.Info("开始购买资源"); }

            // get the list of taiwu villagers id (except taiwu itself ) 
            List<int> villagerIdsList = new List<int>();
            DomainManager.Organization.GetElement_CivilianSettlements(DomainManager.Taiwu.GetTaiwuVillageSettlementId()).GetMembers().GetAllMembers(villagerIdsList);
            villagerIdsList.Remove(DomainManager.Taiwu.GetTaiwuCharId());

            // get taiwu 
            GameData.Domains.Character.Character taiwu;
            int taiwuId = DomainManager.Taiwu.GetTaiwuCharId();

            bool isTaiwuFound = DomainManager.Character.TryGetElement_Objects(taiwuId, out taiwu);
            if (!isTaiwuFound)
            {
                if (debugInfo) AdaptableLog.Warning("未找到太吾");
                return;
            }

            // which resources does taiwu need ? 
            List<sbyte> lackResources = new List<sbyte>();

            // food, wood, metal, jade, fabric, plants
            sbyte resourceTypesCount = 6;
            int upperLimit = DomainManager.Taiwu.GetMaterialResourceMaxCount();

            // TODO may change
            double threshold = 0.8 * upperLimit;
            if (debugInfo) { AdaptableLog.Info(String.Format("当前仓库上限{0}，仓库能接受的最大资源上限是{1}", upperLimit, threshold)); }
            if (debugInfo) { AdaptableLog.Info(String.Format("太吾目前拥有{0}资源6", taiwu.GetResource(6))); }
            if (debugInfo) { AdaptableLog.Info(String.Format("太吾目前拥有{0}资源7", taiwu.GetResource(7))); }
            for (sbyte i = 0; i < resourceTypesCount; i++)
            {
                int resource = taiwu.GetResource(i);
                if (debugInfo) { AdaptableLog.Info(String.Format("太吾目前拥有{0}资源{1}", resource, i)); }
                if (resource < threshold)
                {
                    lackResources.Add(i);
                    if (debugInfo) { AdaptableLog.Info(String.Format("太吾缺{0}资源{1}", threshold - resource, i)); }
                }
            }

            // taiwu does not need resources
            if (lackResources.Count == 0)
            {
                return;
            }

            // taiwu pay with money 
            int taiwuBudget = taiwu.GetResource(6) * taiwuBudgetRate / 100;
            if (debugInfo) { AdaptableLog.Info(String.Format("太吾准备了{0}资源6", taiwuBudget)); }


            // taiwu pay with renown
            int taiwuRenown = taiwu.GetResource(7) * taiwuRenownRate / 100;
            if (debugInfo) { AdaptableLog.Info(String.Format("太吾准备了{0}资源7", taiwuRenown)); }

            int initialBudget = taiwuBudget;
            int initialRenown = taiwuRenown;

            // flag means the strategy that taiwu is going to use to buy resources
            // 0 : overflow resources
            // 1 : renown 
            // 2 : money 
            // 3 : storage is full or taiwu cant buy 
            int strategy = 0;


           
            // TODO, run the indexes randomly 
            foreach (int id in villagerIdsList)
            {

                // get villager
                GameData.Domains.Character.Character villager;
                bool exists = DomainManager.Character.TryGetElement_Objects(id, out villager);
                if (!exists)
                {
                    continue;
                }
                if (debugInfo) AdaptableLog.Info(String.Format("准备向村民{0}购买资源", villager.GetId()));

                if (strategy == 3) {
                    if (debugInfo) AdaptableLog.Info(String.Format("太吾购买完毕"));

                    break; ; 
                }

                bool enought = true;
                foreach (sbyte i in lackResources)
                {
                    int lackQuantity = (int)threshold - taiwu.GetResource(i);

                    if (lackQuantity < 1)
                    {

                        continue;
                    }

                    enought = false;

                    // the villager should keep a minimal plants for it/her/iel-self
                    if (i == 5 && villager.GetResource(i) < minimumPlants + 1)
                    {
                        continue;
                    }

                    if (villager.GetResource(i) < 1)
                    {
                        continue;
                    }

                    if (strategy == 0)
                    {
                        BuyWithOverflowResources(context, taiwu, villager, i, threshold, ref strategy);
                    }

                    if (strategy == 1)
                    {
                        BuyWithRenown(context, taiwu, villager, i, threshold, ref taiwuRenown, ref strategy);
                    }

                    if (strategy == 2)
                    {
                        BuyWithMoney(context, taiwu, villager, i, threshold, ref taiwuBudget, ref strategy);
                    }



                }

                if (enought)
                {
                    strategy = 3;
                }

              

            }
            if (debugInfo) AdaptableLog.Info(String.Format("太吾算账单"));
            DomainManager.World.GetInstantNotificationCollection().AddResourceDecreased(DomainManager.Taiwu.GetTaiwuCharId(), 6, initialBudget - taiwuBudget);
            DomainManager.World.GetInstantNotificationCollection().AddResourceDecreased(DomainManager.Taiwu.GetTaiwuCharId(), 7, initialRenown - taiwuRenown);

        }


        public static void BuyWithOverflowResources(DataContext context, GameData.Domains.Character.Character taiwu,
                                             GameData.Domains.Character.Character villager, sbyte resourceIndex, double threshold, ref int flag)
        {

            bool anyExtra = false;

            // taiwu pays villager resource with his overflown resource 
            for (sbyte j = 0; j < 6; j++)
            {
                // get the quantity of resource that the villager can offer
                int lackQuantity = (int)threshold - taiwu.GetResource(resourceIndex);
                int resource = villager.GetResource(resourceIndex);
                if (resource < 1)
                {
                    return;
                }
                int surplus = (resourceIndex != 5) ? Math.Min(lackQuantity, resource) : Math.Min(lackQuantity, resource - minimumPlants);
                int extraResource = taiwu.GetResource(j) - (int)threshold;
                if (debugInfo) { AdaptableLog.Info(String.Format("太吾缺{0}的资源{1}", lackQuantity, resourceIndex)); }
                if (debugInfo) { AdaptableLog.Info(String.Format("村民{0}拥有{1}的资源{2}", villager.GetId(), resource, resourceIndex)); }

                if (extraResource < 1)
                {
                    continue;
                }

                anyExtra = anyExtra || true;
                if (debugInfo) { AdaptableLog.Info(String.Format("太吾拥有{0}的溢出资源{1}", extraResource, j)); }

                int exchangeAmount = Math.Min(surplus, extraResource);
                villager.ChangeResource(context, resourceIndex, -exchangeAmount);
                taiwu.ChangeResource(context, resourceIndex, exchangeAmount);
                taiwu.ChangeResource(context, j, -exchangeAmount);
                villager.ChangeResource(context, j, exchangeAmount);
                if (debugInfo) { AdaptableLog.Info(String.Format("村民{0}失去{1}的资源{2}，得到{3}的资源{4}", villager.GetId(), exchangeAmount, resourceIndex, exchangeAmount, j)); }
            }

            if (!anyExtra)
            {
                flag = 1;
            }

        }

        public static void BuyWithRenown(DataContext context, GameData.Domains.Character.Character taiwu,
                     GameData.Domains.Character.Character villager, sbyte resourceIndex, double threshold, ref int taiwuRenown, ref int flag)
        {

            if (taiwuRenown < 3)
            {
                flag = 2;
                return;
            }

            int lackQuantity = (int)threshold - taiwu.GetResource(resourceIndex);
            int resource = villager.GetResource(resourceIndex);

            int surplus = (resourceIndex != 5) ? Math.Min(lackQuantity, resource) : Math.Min(lackQuantity, resource - minimumPlants);

            if (debugInfo) { AdaptableLog.Info(String.Format("村民{0}剩余{1}的资源{2}", villager.GetId(), resource, resourceIndex)); }

            int exchangeAmount = Math.Min(taiwuRenown * 2, surplus);
            villager.ChangeResource(context, resourceIndex, -exchangeAmount);
            taiwu.ChangeResource(context, resourceIndex, exchangeAmount);
            int renownSpent = exchangeAmount / 2;
            taiwuRenown -= renownSpent;

            taiwu.ChangeResource(context, 7, -renownSpent);
            villager.ChangeResource(context, 7, renownSpent);
            if (debugInfo) { AdaptableLog.Info(String.Format("村民{0}失去{1}的资源{2}，得到{3}的资源7", villager.GetId(), exchangeAmount, resourceIndex, renownSpent)); }
            if (debugInfo) { AdaptableLog.Info(String.Format("太吾剩余威望成本{0}", taiwuRenown)); }
        }




        public static void BuyWithMoney(DataContext context, GameData.Domains.Character.Character taiwu,
                                     GameData.Domains.Character.Character villager, sbyte resourceIndex, double threshold, ref int taiwuBudget, ref int flag)
        {

            if (taiwuBudget < 5)
            {
                flag = 3;
                return;
            }

            int lackQuantity = (int)threshold - taiwu.GetResource(resourceIndex);
            int resource = villager.GetResource(resourceIndex);

            int surplus = (resourceIndex != 5) ? Math.Min(lackQuantity, resource) : Math.Min(lackQuantity, resource - minimumPlants);

            if (debugInfo) { AdaptableLog.Info(String.Format("村民{0}剩余{1}的资源{2}", villager.GetId(), resource, resourceIndex)); }

            int exchangeAmount = Math.Min(taiwuBudget / 4, surplus);
            villager.ChangeResource(context, resourceIndex, -exchangeAmount);
            taiwu.ChangeResource(context, resourceIndex, exchangeAmount);
            int moneySpent = 4 * exchangeAmount;
            taiwuBudget -= moneySpent;

            taiwu.ChangeResource(context, 6, -moneySpent);
            villager.ChangeResource(context, 6, moneySpent);
            if (debugInfo) { AdaptableLog.Info(String.Format("村民{0}失去{1}的资源{2}，得到{3}的资源6", villager.GetId(), exchangeAmount, resourceIndex, moneySpent)); }
            if (debugInfo) { AdaptableLog.Info(String.Format("太吾剩余金钱成本{0}", taiwuBudget)); }
        }





    }

}
