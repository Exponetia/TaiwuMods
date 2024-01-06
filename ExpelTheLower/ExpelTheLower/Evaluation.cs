using GameData.Common;
using GameData.Domains;
using GameData.Domains.Taiwu;
using GameData.Domains.Character.AvatarSystem;
using System.Linq;
using GameData.Utilities;
using System;


namespace ExpelTheLower
{
    public class Evaluation
    {
        // minimal accepting charm
        public static int minimalCharm;

        public static int minimalLifeSkillAttainment;
        public static int minimalLifeSkillQualification;
         
        public static int minimalCombatSkillAttainment;
        public static int minimalCombatSkillQualification;

        public static int maximalAge;

        // the personality to fire 
        public static int forbiddenBehavior;

        // print the debug info 
        public static bool debugInfo;

        public static void GetRidTheLower(DataContext context)
        {
            if (debugInfo) { AdaptableLog.Info("开始驱逐"); }

            // get the list of taiwu villagers id (except taiwu itself ) 
            List<int> villagerIdsList = new List<int>();
            DomainManager.Organization.GetElement_CivilianSettlements(DomainManager.Taiwu.GetTaiwuVillageSettlementId()).GetMembers().GetAllMembers(villagerIdsList);
            villagerIdsList.Remove(DomainManager.Taiwu.GetTaiwuCharId());
            IEnumerable<int> expelledVillager = villagerIdsList.Where(id => ShouldExpel(id));

            // get taiwu 
            GameData.Domains.Character.Character taiwu;
            int taiwuId = DomainManager.Taiwu.GetTaiwuCharId();

            bool isTaiwuFound = DomainManager.Character.TryGetElement_Objects(taiwuId, out taiwu);
            if (!isTaiwuFound)
            {
                if (debugInfo) AdaptableLog.Warning("未找到太吾");
                return;
            }

            foreach (int id in expelledVillager)
            {
                
                DomainManager.Taiwu.ExpelVillager(context, id);
                if (debugInfo) { AdaptableLog.Info(String.Format("驱逐村民{0}", id)); }
            }
        }

        public static bool AreParametersSet()
        {
            return minimalCharm > -1 || forbiddenBehavior > -1 || 
                   minimalLifeSkillAttainment > -1 || minimalLifeSkillQualification > -1 || 
                   minimalCombatSkillAttainment > -1 || minimalCombatSkillQualification > -1 || maximalAge > 15;
        }

        private static bool ShouldExpel(int id)
        {
            return !IsBaby(id) && 
                    IsCharmBelow(id) && IsYoungEnough(id) &&
                    IsBehaviorForbidden(id) && 
                    IsLifeSkillQualificationEnought(id) && IsLifeSkillAttainmentEnought(id) &&
                    IsCombatSkillQualificationEnought(id) && IsCombatSkillAttainmentEnought(id) &&
                    !IsInGroup(id);
        }


        private static bool IsInGroup(int id) { return DomainManager.Taiwu.IsInGroup(id); }

        private static bool IsBaby(int id)
        {
            GameData.Domains.Character.Character character = DomainManager.Character.GetElement_Objects(id);
            return character.GetAgeGroup() == 0 || character.GetAgeGroup() == 1;
        }

        private static bool IsYoungEnough(int id)
        {
            if (maximalAge < 16) { return true; }
            DomainManager.Character.TryGetElement_Objects(id, out GameData.Domains.Character.Character villager);
            if (debugInfo) { AdaptableLog.Info(String.Format("开始审判村民{0}名为{1}的年龄", id, villager.GetGivenName())); }
            int age = villager.GetCurrAge();
            if (debugInfo) { AdaptableLog.Info(String.Format("村民{0}名为{1}的年龄为{2}", id, villager.GetGivenName(), age)); }
            return age > maximalAge;
        }


        private static bool IsCharmBelow(int id)
        {
            if (minimalCharm < 0) { return true; }

            DomainManager.Character.TryGetElement_Objects(id, out GameData.Domains.Character.Character villager);
            if (debugInfo) { AdaptableLog.Info(String.Format("开始审判村民{0}名为{1}的基础魅力", id, villager.GetGivenName())); }

            // get the real charm by ignoring the hair, beard, trait, clothes features ..
            AvatarData VillagerAvatar = villager.GetAvatar();
            int charm = VillagerAvatar.GetBaseCharm();
            if (debugInfo) { AdaptableLog.Info(String.Format("村民{0}名为{1}的基础魅力为{2}", id, villager.GetGivenName(), charm)); }
            return charm < minimalCharm;
        }

        private static bool IsBehaviorForbidden(int id)
        {
            if (forbiddenBehavior < 0) { return true; }
            DomainManager.Character.TryGetElement_Objects(id, out GameData.Domains.Character.Character villager);
            if (debugInfo) { AdaptableLog.Info(String.Format("开始审判村民{0}名为{1}的立场", id, villager.GetGivenName())); }
            int behavior = villager.GetBehaviorType();
            if (debugInfo) { AdaptableLog.Info(String.Format("村民{0}名为{1}的立场为{2}", id, villager.GetGivenName(), behavior)); }
            return behavior == forbiddenBehavior;
        }

        private static bool IsLifeSkillQualificationEnought(int id)
        {
            if (minimalLifeSkillQualification < 0) { return true; }
            DomainManager.Character.TryGetElement_Objects(id, out GameData.Domains.Character.Character villager);
            if (debugInfo) { AdaptableLog.Info(String.Format("开始审判村民{0}名为{1}的技艺资质", id, villager.GetGivenName())); }
            int lifeSkillQualification = villager.GetBaseLifeSkillQualifications().GetMaxLifeSkillValue();
            if (debugInfo) { AdaptableLog.Info(String.Format("村民{0}名为{1}的最高技艺资质为{2}", id, villager.GetGivenName(), lifeSkillQualification)); }
            return lifeSkillQualification < minimalLifeSkillQualification;
        }

        private static bool IsLifeSkillAttainmentEnought(int id)
        {   
            if (minimalLifeSkillAttainment < 0 ) { return true; }
            DomainManager.Character.TryGetElement_Objects(id, out GameData.Domains.Character.Character villager);
            if (debugInfo) { AdaptableLog.Info(String.Format("开始审判村民{0}名为{1}的技艺造诣", id, villager.GetGivenName())); }
            int lifeSkillAttainment = villager.GetMaxLifeSkillAttainment();
            if (debugInfo) { AdaptableLog.Info(String.Format("村民{0}名为{1}的最高技艺造诣为{2}", id, villager.GetGivenName(), lifeSkillAttainment)); }
            return lifeSkillAttainment < minimalLifeSkillAttainment;
        }

        private static bool IsCombatSkillQualificationEnought(int id)
        {
            if (minimalCombatSkillQualification < 0) { return true; }
            DomainManager.Character.TryGetElement_Objects(id, out GameData.Domains.Character.Character villager);
            if (debugInfo) { AdaptableLog.Info(String.Format("开始审判村民{0}名为{1}的武技资质", id, villager.GetGivenName())); }
            int combatSkillQualification = villager.GetBaseCombatSkillQualifications().GetMaxCombatSkillValue();
            if (debugInfo) { AdaptableLog.Info(String.Format("村民{0}名为{1}的最高武技资质为{2}", id, villager.GetGivenName(), combatSkillQualification)); }
            return combatSkillQualification < minimalCombatSkillQualification;
        }

        private static bool IsCombatSkillAttainmentEnought(int id)
        {
            if (minimalCombatSkillAttainment < 0 ) { return true; }
            DomainManager.Character.TryGetElement_Objects(id, out GameData.Domains.Character.Character villager);
            if (debugInfo) { AdaptableLog.Info(String.Format("开始审判村民{0}名为{1}的武技造诣", id, villager.GetGivenName())); }
            int combatSkillAttainment = villager.GetMaxCombatSkillAttainment();
            if (debugInfo) { AdaptableLog.Info(String.Format("村民{0}名为{1}的最高武技造诣为{2}", id, villager.GetGivenName(), combatSkillAttainment)); }
            return combatSkillAttainment < minimalCombatSkillAttainment;
        }
    }
}