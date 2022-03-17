// ;
using Sandbox.Game;
using Sandbox.ModAPI;
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
        private List<MyStringHash> m_Inventory_Plugins = null;

        private MyStringHash m_Inventory_ManualEmitterItems = MyStringHash.NullOrEmpty;
        private MyStringHash m_Inventory_AutoEmitterItems = MyStringHash.NullOrEmpty;
        
        private void Inventory_BeforeContentsChanged(MyInventoryBase _inventory)
        {
            IMyCharacter character = _inventory.Container.Entity as IMyCharacter;
            if (character == null)
                return;

            if (character.IsDead)
            {
                m_Logger.WriteLine("Character [" + Utils.GetCharacterName(character) + "] is dead", 5);
                return;
            }

            CharacterShieldInfo charInfo = m_ShieldManager.CharacterInfos[character.EntityId];
            if (charInfo.AutoEmitter == null)
            {
                m_Logger.WriteLine("Character [" + Utils.GetCharacterName(character) + "] doesn't have Auto Emitter", 2);
                return;
            }

            m_Logger.WriteLine("Inventory of character [" + Utils.GetCharacterName(character) + "]'s content is about to change", 5);
            
            List<MyPhysicalInventoryItem> inventoryItems = _inventory.GetItems();
            m_Logger.WriteLine("Processing Inventory items..", 5);
            for (int i = 0; i < inventoryItems.Count; ++i)
            {
                MyStringHash subtypeId = inventoryItems[i].Content.SubtypeId;
                m_Logger.WriteLine("  Processing item " + subtypeId, 4);

                if (charInfo.AutoEmitter.SubtypeId == subtypeId)
                {
                    charInfo.AutoEmitterIndex = i;
                    break;
                }
            }
        }

        private void Inventory_ContentsChanged(MyInventoryBase _inventory)
        {
            IMyCharacter character = _inventory.Container.Entity as IMyCharacter;
            if (character == null)
                return;

            if (character.IsDead)
            {
                m_Logger.WriteLine("Character [" + Utils.GetCharacterName(character) + "] is dead", 5);
                return;
            }

            m_Logger.WriteLine("Inventory of character [" + Utils.GetCharacterName(character) + "]'s content has changed", 5);

            Inventory_RefreshInventory(_inventory);
        }

        private void Inventory_RefreshInventory(MyInventoryBase _inventory)
        {
            m_Logger.WriteLine("===== Starting Inventory_RefreshInventory() =====", 4);

            IMyCharacter character = _inventory.Container.Entity as IMyCharacter;
            if (character == null)
                return;
            
            List<MyPhysicalInventoryItem> inventoryItems = _inventory.GetItems();
            m_Logger.WriteLine("[" + Utils.GetCharacterName(character) + "]'s inventory now contains " + inventoryItems.Count + " items.", 4);

            m_Logger.WriteLine("Processing Inventory items..", 5);
            int manualInd = -1;
            int autoInd = -1;
            for (int i = 0; i < inventoryItems.Count; ++i)
            {
                MyPhysicalInventoryItem item = inventoryItems[i];
                MyStringHash subtypeId = item.Content.SubtypeId;
                m_Logger.WriteLine("  Processing item " + subtypeId, 4);

                if (!subtypeId.String.Contains("PocketShield_"))
                    continue;
                
                if (subtypeId.String.Contains("Emitter"))
                {
                    PocketShieldAPIV2.ShieldEmitterProperties prop = m_ShieldManager.GetEmitterProperties(subtypeId);
                    if (prop == null)
                        continue;

                    if (prop.IsManual)
                    {
                        if (m_Inventory_ManualEmitterItems == MyStringHash.NullOrEmpty)
                        {
                            m_Logger.WriteLine("    Accepted Manual Emitter " + subtypeId, 4);
                            m_Inventory_ManualEmitterItems = subtypeId;
                            manualInd = i;
                        }
                    }
                    else
                    {
                        if (m_Inventory_AutoEmitterItems == MyStringHash.NullOrEmpty)
                        {
                            m_Logger.WriteLine("    Accepted Auto Emitter " + subtypeId, 4);
                            m_Inventory_AutoEmitterItems = subtypeId;
                            autoInd = i;
                        }
                    }
                }
                else if (subtypeId.String.Contains("Plugin"))
                {
                    m_Logger.WriteLine("    Accepted " + item.Amount + " Plugin", 4);
                    for (int j = 0; j < item.Amount; ++j)
                        m_Inventory_Plugins.Add(subtypeId);
                }
                else
                {
                    m_Logger.WriteLine("    Hmm, unknown PocketShield item " + subtypeId, 4);
                }
            }

            m_Logger.WriteLine("Processing Emitter items (if found any)..", 5);
            ProcessManualEmitter(character);
            ProcessAutoEmitter(character);

            //if (manualInd != -1)
            //    m_ShieldManager.CharacterInfos[character.EntityId].ManualEmitterIndex = manualInd;
            //if (autoInd != -1)
            //    m_ShieldManager.CharacterInfos[character.EntityId].AutoEmitterIndex = autoInd;

            //m_Logger.WriteLine("Auto Index = " + autoInd + ", set = " + m_ShieldManager.CharacterInfos[character.EntityId].AutoEmitterIndex);

            m_Logger.WriteLine("Cleaning up..", 5);
            m_Inventory_Plugins.Clear();
            m_Inventory_ManualEmitterItems = MyStringHash.NullOrEmpty;
            m_Inventory_AutoEmitterItems = MyStringHash.NullOrEmpty;

            m_Logger.WriteLine("===== Ending Inventory_RefreshInventory()   =====", 4);
        }

        private void ProcessManualEmitter(IMyCharacter _character)
        {
            m_Logger.Write("  Processing Manual Emitter: ", 4);

            ShieldEmitter oldEmitter = m_ShieldManager.CharacterInfos[_character.EntityId].ManualEmitter;
            ShieldEmitter newEmitter = null;

            if (oldEmitter == null)
            {
                m_Logger.WriteInline("Old Emitter is null, ", 4);
                if (m_Inventory_ManualEmitterItems == MyStringHash.NullOrEmpty)
                {
                    m_Logger.WriteInline("no new emitter -> do nothing\n", 4);
                }
                else
                {
                    m_Logger.WriteInline("new emitter detected -> add emitter\n", 4);
                    newEmitter = m_ShieldManager.AddNewEmitter(_character, m_Inventory_ManualEmitterItems, true);
                    if (newEmitter != null)
                        newEmitter.Energy = m_SaveData.GetSavedManualShieldEnergy(_character.EntityId);
                }
            }
            else
            {
                m_Logger.WriteInline("Has old emitter, ", 4);
                if (m_Inventory_ManualEmitterItems == MyStringHash.NullOrEmpty)
                {
                    m_Logger.WriteInline("no new emitter -> drop emitter\n", 4);
                    m_ShieldManager.DropEmitter(_character, false);

                    ulong playerUid = GetPlayerSteamUid(_character);
                    if (!m_ForceSyncPlayers.Contains(playerUid))
                        m_ForceSyncPlayers.Add(playerUid);
                }
                else
                {
                    m_Logger.WriteInline("new emitter detected -> ", 4);
                    if (oldEmitter.SubtypeId == m_Inventory_ManualEmitterItems)
                    {
                        m_Logger.WriteInline("do nothing\n", 4);
                        newEmitter = oldEmitter;
                    }
                    else
                    {
                        m_Logger.WriteInline("replace\n", 4);
                        newEmitter = m_ShieldManager.ReplaceEmitter(_character, m_Inventory_ManualEmitterItems, true);
                    }
                }
            }

            if (newEmitter != null)
            {
                if (m_Inventory_Plugins.Count > 0)
                    newEmitter.AddPlugins(ref m_Inventory_Plugins);
            }
        }

        private void ProcessAutoEmitter(IMyCharacter _character)
        {
            m_Logger.Write("  Processing Auto Emitter: ", 4);

            ShieldEmitter oldEmitter = m_ShieldManager.CharacterInfos[_character.EntityId].AutoEmitter;
            ShieldEmitter newEmitter = null;
            
            if (oldEmitter == null)
            {
                m_Logger.WriteInline("Old Emitter is null, ", 4);
                if (m_Inventory_AutoEmitterItems == MyStringHash.NullOrEmpty)
                {
                    m_Logger.WriteInline("no new emitter -> do nothing\n", 4);
                }
                else
                {
                    m_Logger.WriteInline("new emitter detected -> add emitter\n", 4);
                    newEmitter = m_ShieldManager.AddNewEmitter(_character, m_Inventory_AutoEmitterItems, false);
                    if (newEmitter != null)
                    {
                        newEmitter.Energy = m_SaveData.GetSavedAutoShieldEnergy(_character.EntityId);
                        newEmitter.IsTurnedOn = m_SaveData.GetSavedAutoShieldTurnedOn(_character.EntityId);
                    }
                }
            }
            else
            {
                m_Logger.WriteInline("Has old emitter, ", 4);
                if (m_Inventory_AutoEmitterItems == MyStringHash.NullOrEmpty)
                {
                    m_Logger.WriteInline("no new emitter -> drop emitter\n", 4);
                    m_ShieldManager.DropEmitter(_character, false);

                    ulong playerUid = GetPlayerSteamUid(_character);
                    if (!m_ForceSyncPlayers.Contains(playerUid))
                        m_ForceSyncPlayers.Add(playerUid);
                }
                else
                {
                    m_Logger.WriteInline("new emitter detected -> ", 4);
                    if (oldEmitter.SubtypeId == m_Inventory_AutoEmitterItems)
                    {
                        m_Logger.WriteInline("do nothing\n", 4);
                        newEmitter = oldEmitter;
                    }
                    else
                    {
                        m_Logger.WriteInline("replace\n", 4);
                        newEmitter = m_ShieldManager.ReplaceEmitter(_character, m_Inventory_AutoEmitterItems, false);
                    }
                }
            }
            
            if (newEmitter != null)
            {
                if (m_Inventory_Plugins.Count > 0)
                    newEmitter.AddPlugins(ref m_Inventory_Plugins);
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
            m_Logger.WriteLine("> UpdatePlayerCharacterInventoryOnceBeforeSim()..", 4);
            //UpdatePlayersCacheLists();
            MyAPIGateway.Players.GetPlayers(m_CachedPlayers);
            foreach (IMyPlayer player in m_CachedPlayers)
            {
                if (player.Character == null)
                    continue;

                if (!m_HookedEntities.ContainsKey(player.Character.Name))
                    HookEntity(player.Character);

                //if (!m_ShieldManager.CharacterInfos.ContainsKey(player.Character.EntityId))
                //{
                //    m_Logger.WriteLine("  Character [" + Utils.GetCharacterName(player.Character) + "] is not registered with ShieldManager yet (maybe character is in seat?)", 2);
                //    m_ShieldManager.CharacterInfos[player.Character.EntityId] = new CharacterShieldInfo();
                //}

                m_Logger.WriteLine(">   Updating player " + player.SteamUserId, 4);
                if (player.Character == null || !player.Character.HasInventory)
                    continue;

                var inventory = player.Character.GetInventory() as MyInventory;
                if (inventory == null)
                    continue;

                Inventory_RefreshInventory(inventory);
                m_Logger.WriteLine(">     Player Character Inventory updated", 4);
            }
        }



    }
}
