// ;
using ExShared;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;
using VRage.Utils;

namespace PocketShieldCore
{
    public partial class Session_PocketShieldCoreServer : MySessionComponentBase
    {
        private Dictionary<MyStringHash, float> m_CachedPrice = new Dictionary<MyStringHash, float>(MyStringHash.Comparer);

        private void Blueprints_UpdateBlueprintData(bool _force = false)
        {
            if (_force)
                m_Logger.WriteLine("Updating Item price (force!)..", 1);
            else
                m_Logger.WriteLine("Updating Item price..", 5);

            var physItemDef = MyDefinitionManager.Static.GetPhysicalItemDefinitions();
            foreach (var def in physItemDef)
            {
                if (def.Id.SubtypeId.String.Contains("PocketShield_"))
                {
                    if (!m_CachedPrice.ContainsKey(def.Id.SubtypeId) || _force)
                    {
                        m_CachedPrice[def.Id.SubtypeId] = CalculateItemMinimalPrice(def.Id) * 0.5f;
                        def.MinimalPricePerUnit = (int)m_CachedPrice[def.Id.SubtypeId];
                    }
                }
            }
        }

        // HACK: copied from Sandbox.Game.World.Generator.MyMinimalPriceCalculator.CalculateItemMinimalPrice(VRage.Game.MyDefinitionId, float, ref int);
        private float CalculateItemMinimalPrice(MyDefinitionId _id)
        {
            MyPhysicalItemDefinition physItemDef = null;
            if (MyDefinitionManager.Static.TryGetPhysicalItemDefinition(_id, out physItemDef) && physItemDef.MinimalPricePerUnit != -1)
            {
                return physItemDef.MinimalPricePerUnit;
            }

            MyBlueprintDefinitionBase bpDefBase = null;
            if (!MyDefinitionManager.Static.TryGetBlueprintDefinitionByResultId(_id, out bpDefBase))
            {
                return 0.0f;
            }

            float price = 0.0f;
            float efficiencyMod = physItemDef.IsIngot ? 1.0f : MyAPIGateway.Session.AssemblerEfficiencyMultiplier;
            foreach (var item in bpDefBase.Prerequisites)
            {
                price += CalculateItemMinimalPrice(item.Id) * (float)item.Amount / efficiencyMod;
            }

            float speedMod = physItemDef.IsIngot ? MyAPIGateway.Session.RefinerySpeedMultiplier : MyAPIGateway.Session.AssemblerSpeedMultiplier;
            for (int i = 0; i < bpDefBase.Results.Length; ++i)
            {
                var item = bpDefBase.Results[i];
                if (item.Id == _id && (float)item.Amount > 0.0f)
                {
                    // this is the item we want to get;

                    float number = 1.0f + (float)Math.Log(bpDefBase.BaseProductionTimeInSeconds + 1.0f) / speedMod;
                    price *= (1.0f / (float)item.Amount) * number;
                    return price;
                }
            }

            return 0.0f;
        }
        

    }
}
