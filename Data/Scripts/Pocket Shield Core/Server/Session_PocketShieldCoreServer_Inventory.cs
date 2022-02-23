// ;
using Sandbox.Game;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace PocketShieldCore
{
    public partial class Session_PocketShieldCoreServer
    {
        private List<MyStringHash> m_Inventory_Plugins = new List<MyStringHash>();

        private MyStringHash m_Inventory_ManualEmitterItems = MyStringHash.NullOrEmpty;
        private MyStringHash m_Inventory_AutoEmitterItems = MyStringHash.NullOrEmpty;
        
        private void Inventory_ContentsChangedHandle(MyInventoryBase _inventory)
        {
            IMyCharacter character = _inventory.Container.Entity as IMyCharacter;
            if (character == null)
                return;

            if (character.IsDead)
            {
                m_Logger.WriteLine("Character [" + Utils.GetCharacterName(character) + "] is dead");
                return;
            }

            m_Logger.WriteLine("Inventory of character [" + Utils.GetCharacterName(character) + "]'s content has changed", 5);

            Inventory_RefreshInventory(_inventory);
        }

        private void Inventory_RefreshInventory(MyInventoryBase _inventory)
        {
            m_Logger.WriteLine("Starting RefreshInventory()", 4);

            IMyCharacter character = _inventory.Container.Entity as IMyCharacter;
            if (character == null)
                return;

            List<MyPhysicalInventoryItem> InventoryItems = _inventory.GetItems();
            m_Logger.WriteLine("  [" + Utils.GetCharacterName(character) + "]'s inventory now contains " + InventoryItems.Count + " items.", 4);

            foreach (MyPhysicalInventoryItem item in InventoryItems)
            {
                MyStringHash subtypeId = item.Content.SubtypeId;
                m_Logger.WriteLine("  Processing item " + subtypeId, 4);

                if (!subtypeId.String.Contains("PocketShield_"))
                    continue;

                if (subtypeId.String.Contains("Emitter"))
                {
                    if (!m_ShieldManager_EmitterConstructionData.ContainsKey(subtypeId))
                        continue;

                    m_CachedProps.ReplaceData(m_ShieldManager_EmitterConstructionData[subtypeId]);
                    if (m_CachedProps.IsManual)
                    {
                        if (m_Inventory_ManualEmitterItems == MyStringHash.NullOrEmpty)
                        {
                            m_Logger.WriteLine("    Accepted Manual Emitter " + subtypeId, 4);
                            m_Inventory_ManualEmitterItems = subtypeId;
                        }
                    }
                    else
                    {
                        if (m_Inventory_AutoEmitterItems == MyStringHash.NullOrEmpty)
                        {
                            m_Logger.WriteLine("    Accepted Auto Emitter " + subtypeId, 4);
                            m_Inventory_AutoEmitterItems = subtypeId;
                        }
                    }
                }
                else if (subtypeId.String.Contains("Plugin"))
                {
                    m_Logger.WriteLine("    Accepted " + item.Amount + " Plugin", 4);
                    for (int i = 0; i < item.Amount; ++i)
                        m_Inventory_Plugins.Add(subtypeId);
                }
                else
                {
                    m_Logger.WriteLine("    Hmm, unknown PocketShield item " + subtypeId, 4);
                }
            }

            ProcessManualEmitter(character);
            ProcessAutoEmitter(character);

            // cleanup cache;
            m_Inventory_Plugins.Clear();
            m_Inventory_ManualEmitterItems = MyStringHash.NullOrEmpty;
            m_Inventory_AutoEmitterItems = MyStringHash.NullOrEmpty;
            return;
        }

        private void ProcessManualEmitter(IMyCharacter _character)
        {
            ShieldEmitter oldEmitter = null;
            ShieldEmitter newEmitter = null;

            m_Logger.WriteLine("  Processing Manual Emitter..", 4);
            if (m_ShieldManager_CharacterShieldManagers.ContainsKey(_character.EntityId))
                oldEmitter = m_ShieldManager_CharacterShieldManagers[_character.EntityId].ManualEmitter;

            bool replaced = false;
            if (oldEmitter == null)
            {
                m_Logger.Write("    Old Emitter is null, ", 4);
                if (m_Inventory_ManualEmitterItems == MyStringHash.NullOrEmpty)
                {
                    m_Logger.WriteInline("no new emitter -> do nothing\n", 4);
                }
                else
                {
                    m_Logger.WriteInline("new emitter detected -> add emitter\n", 4);
                    newEmitter = ShieldManager_AddNewEmitter(_character, m_Inventory_ManualEmitterItems, true);
                }
            }
            else
            {
                m_Logger.Write("    Has old emitter, ", 4);
                if (m_Inventory_ManualEmitterItems == MyStringHash.NullOrEmpty)
                {
                    m_Logger.WriteInline("no new emitter -> drop emitter\n", 4);
                    ShieldManager_DropEmitter(_character, true);
                }
                else
                {
                    m_Logger.WriteInline("new emitter detected -> replace emitter or do nothing\n", 4);
                    if (oldEmitter.SubtypeId == m_Inventory_ManualEmitterItems)
                        newEmitter = oldEmitter;
                    else
                    {
                        newEmitter = ShieldManager_ReplaceEmitter(_character, m_Inventory_ManualEmitterItems, true);
                        replaced = true;
                    }
                }
            }


            if (newEmitter != null)
            {
                if (m_Inventory_Plugins.Count > 0)
                    newEmitter.AddPlugins(ref m_Inventory_Plugins);

                if (!replaced && m_SaveData != null)
                    newEmitter.Energy = m_SaveData.GetSavedManualShieldEnergy(_character.EntityId);
            }
        }

        private void ProcessAutoEmitter(IMyCharacter _character)
        {
            ShieldEmitter oldEmitter = null;
            ShieldEmitter newEmitter = null;

            m_Logger.WriteLine("  Processing Auto Emitter..", 4);
            if (m_ShieldManager_CharacterShieldManagers.ContainsKey(_character.EntityId))
                oldEmitter = m_ShieldManager_CharacterShieldManagers[_character.EntityId].AutoEmitter;

            bool replaced = false;
            if (oldEmitter == null)
            {
                m_Logger.Write("    Old Emitter is null, ", 4);
                if (m_Inventory_AutoEmitterItems == MyStringHash.NullOrEmpty)
                {
                    m_Logger.WriteInline("no new emitter -> do nothing\n", 4);
                }
                else
                {
                    m_Logger.WriteInline("new emitter detected -> add emitter\n", 4);
                    newEmitter = ShieldManager_AddNewEmitter(_character, m_Inventory_AutoEmitterItems, false);
                }
            }
            else
            {
                m_Logger.Write("    Has old emitter, ", 4);
                if (m_Inventory_AutoEmitterItems == MyStringHash.NullOrEmpty)
                {
                    m_Logger.WriteInline("no new emitter -> drop emitter\n", 4);
                    ShieldManager_DropEmitter(_character, false);
                }
                else
                {
                    m_Logger.WriteInline("new emitter detected -> replace emitter or do nothing\n", 4);
                    if (oldEmitter.SubtypeId == m_Inventory_AutoEmitterItems)
                        newEmitter = oldEmitter;
                    else
                    {
                        newEmitter = ShieldManager_ReplaceEmitter(_character, m_Inventory_AutoEmitterItems, false);
                        replaced = true;
                    }
                }
            }

            if (newEmitter != null)
            {
                if (m_Inventory_Plugins.Count > 0)
                    newEmitter.AddPlugins(ref m_Inventory_Plugins);

                if (!replaced && m_SaveData != null)
                    newEmitter.Energy = m_SaveData.GetSavedAutoShieldEnergy(_character.EntityId);
            }
        }

        private void Inventory_ConvertDeadCharacterInventory(MyInventory _inventory)
        {
            NpcInventoryOperation flag = m_Config.NpcInventoryOperationOnDeath;
            float ratio = m_Config.NpcShieldItemToCreditRatio;
            float refundAmount = 0.0f;

            List<MyPhysicalInventoryItem> items = _inventory.GetItems();
            for (int i = items.Count - 1; i >= 0; --i)
            {
                MyPhysicalInventoryItem item = items[i];
                if (!item.Content.SubtypeId.String.Contains("PocketShield_"))
                    continue;

                if (item.Content.SubtypeId.String.Contains("Emitter") && (flag & NpcInventoryOperation.RemoveEmitterOnly) != 0)
                {
                    if (ratio > 0.0f)
                    {
                        refundAmount += m_CachedPrice[item.Content.SubtypeId] * ratio;
                        m_Logger.WriteLine("Item " + item.Content.SubtypeId.String + ": " + m_CachedPrice[item.Content.SubtypeId] + " -> " + refundAmount, 4);
                    }
                    _inventory.RemoveItemsAt(i, 1, false);
                }
                else if (item.Content.SubtypeId.String.Contains("Plugin") && (flag & NpcInventoryOperation.RemovePluginOnly) != 0)
                {
                    if (ratio > 0.0f)
                    {
                        refundAmount += (float)item.Amount * m_CachedPrice[item.Content.SubtypeId] * ratio;
                        m_Logger.WriteLine("Item " + item.Content.SubtypeId.String + ": " + m_CachedPrice[item.Content.SubtypeId] + " -> " + refundAmount, 4);
                    }
                    _inventory.RemoveItemsAt(i, item.Amount, false);
                }
            }

            if (refundAmount > 0.0f)
            {
                _inventory.AddItems((int)refundAmount, MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_PhysicalObject>("SpaceCredit"));
                m_Logger.WriteLine("Refunded: " + refundAmount, 4);
            }
        }

        private void Inventory_UpdatePlayerCharacterInventoryOnceBeforeSim()
        {
            m_Logger.WriteLine(">> UpdatePlayerCharacterInventoryOnceBeforeSim()..", 4);
            UpdatePlayersCacheLists();
            foreach (IMyPlayer player in m_CachedPlayers)
            {
                m_Logger.WriteLine(">>   Updating player " + player.SteamUserId, 4);
                if (player.Character == null || !player.Character.HasInventory)
                    continue;

                var inventory = player.Character.GetInventory() as MyInventory;
                if (inventory == null)
                    continue;

                Inventory_RefreshInventory(inventory);
                m_Logger.WriteLine(">>     Player Character Inventory updated", 4);
            }
        }



    }
}
