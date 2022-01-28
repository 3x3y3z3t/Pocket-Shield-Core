// ;
using ExShared;
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
        private List<MyStringHash> m_Inventory_UnknownItems = new List<MyStringHash>();

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

            ShieldEmitter oldEmitter = ShieldFactory_GetEmitter(character);

            foreach (MyPhysicalInventoryItem item in InventoryItems)
            {
                MyStringHash subtypeId = item.Content.SubtypeId;
                m_Logger.WriteLine("  Processing item " + subtypeId, 4);

                if (!subtypeId.String.Contains("PocketShield_"))
                    continue;

                if (subtypeId.String.Contains("Emitter"))
                {
                    // if another emitter item has been processed before, ignore this emitter item;
                    if (m_FirstEmitterFound != null)
                        continue;

                    if (oldEmitter == null)
                    {
                        m_Logger.WriteLine("    Old Emitter is null, try creating Emitter..", 4);
                        ShieldFactory_TryCreateEmitter(subtypeId, character);
                        //ShieldFactory_TryCreateEmitterDel(subtypeId, character);
                        if (m_FirstEmitterFound != null)
                        {
                            ShieldFactory_ReplaceEmitter(character, m_FirstEmitterFound);
                            oldEmitter = m_FirstEmitterFound;
                            m_Logger.WriteLine("    Emitter Created: " + m_FirstEmitterFound.SubtypeId.String, 4);
                            m_Logger.WriteLine("    Old Emitter:     " + oldEmitter.SubtypeId.String, 4);
                            m_Logger.WriteLine("    Emitter count: " + m_PlayerShieldEmitters.Count + " Player's, " + m_NpcShieldEmitters.Count + " Npc's", 1);
                        }
                    }
                    else
                    {
                        m_Logger.WriteLine("    Old Emitter: " + oldEmitter.SubtypeId.String, 4);
                        if (oldEmitter.SubtypeId != subtypeId)
                        {
                            m_Logger.WriteLine("    Try creating new Emitter..", 4);
                            ShieldFactory_TryCreateEmitter(subtypeId, character);
                            //ShieldFactory_TryCreateEmitterDel(subtypeId, character);

                            if (m_FirstEmitterFound != null)
                            {
                                ShieldFactory_ReplaceEmitter(character, m_FirstEmitterFound);
                                oldEmitter = m_FirstEmitterFound;
                                m_Logger.WriteLine("    Emitter Created: " + m_FirstEmitterFound.SubtypeId.String, 4);
                                m_Logger.WriteLine("    Old Emitter:     " + oldEmitter.SubtypeId.String, 4);
                                m_Logger.WriteLine("    Emitter count: " + m_PlayerShieldEmitters.Count + " Player's, " + m_NpcShieldEmitters.Count + " Npc's", 1);
                            }
                        }
                        else
                        {
                            m_FirstEmitterFound = oldEmitter;
                        }
                    }

                    continue;
                }

                if (subtypeId.String.Contains("Plugin"))
                {
                    m_Logger.WriteLine("    Adding Plugin..", 4);
                    m_Inventory_Plugins.Add(subtypeId);
                    continue;
                }

                m_Logger.WriteLine("    Hmm, unknown PocketShield item..", 4);
                m_Inventory_UnknownItems.Add(subtypeId);
            }

            m_Logger.WriteLine("  Found " + m_Inventory_UnknownItems.Count + " unknown PocketShield items", 4);
            foreach (MyStringHash subtypeid in m_Inventory_UnknownItems)
            {
                m_Logger.WriteLine("    " + subtypeid.String, 4);
            }

            if (oldEmitter != null && m_FirstEmitterFound == null)
            {
                m_Logger.WriteLine("  Emitter dropped: " + oldEmitter.SubtypeId);
                ShieldFactory_ReplaceEmitter(character, null);

                if (GetPlayerSteamUid(character) != 0U)
                    m_ForceSyncPlayers.Add(GetPlayerSteamUid(character));
            }

            if (oldEmitter != null)
            {
                oldEmitter.CleanPluginsList();
                oldEmitter.AddPlugins(m_Inventory_Plugins);
            }

            m_FirstEmitterFound = null;
            m_Inventory_Plugins.Clear();
            m_Inventory_UnknownItems.Clear();
            return;
        }

        private void Inventory_ManipulateDeadCharacterInventory(MyInventory _inventory)
        {
            NpcInventoryOperation flag = m_Config.NpcInventoryOperationOnDeath;
            float ratio = m_Config.NpcShieldItemToCreditRatio;
            float refundAmount = 0.0f;

            List<MyPhysicalInventoryItem> items = _inventory.GetItems();
            for (int i = items.Count - 1; i >= 0; --i)
            {
                if ((items[i].Content.SubtypeId == MyStringHash.GetOrCompute(Constants.SUBTYPEID_EMITTER_BAS) ||
                     items[i].Content.SubtypeId == MyStringHash.GetOrCompute(Constants.SUBTYPEID_EMITTER_ADV)) &&
                    (flag & NpcInventoryOperation.RemoveEmitterOnly) != 0)
                {
                    if (ratio > 0.0f)
                    {
                        refundAmount += m_CachedPrice[items[i].Content.SubtypeId] * ratio;
                        m_Logger.WriteLine("Item " + items[i].Content.SubtypeId.String + ": " + m_CachedPrice[items[i].Content.SubtypeId] + " -> " + refundAmount, 4);
                    }
                    _inventory.RemoveItemsAt(i, 1, false);
                }
                else if ((items[i].Content.SubtypeId == MyStringHash.GetOrCompute(Constants.SUBTYPEID_PLUGIN_CAP) ||
                          items[i].Content.SubtypeId == MyStringHash.GetOrCompute(Constants.SUBTYPEID_PLUGIN_DEF_KI) ||
                          items[i].Content.SubtypeId == MyStringHash.GetOrCompute(Constants.SUBTYPEID_PLUGIN_DEF_EX) ||
                          items[i].Content.SubtypeId == MyStringHash.GetOrCompute(Constants.SUBTYPEID_PLUGIN_RES_KI) ||
                          items[i].Content.SubtypeId == MyStringHash.GetOrCompute(Constants.SUBTYPEID_PLUGIN_RES_EX)) &&
                         (flag & NpcInventoryOperation.RemovePluginOnly) != 0)
                {
                    if (ratio > 0.0f)
                    {
                        refundAmount += m_CachedPrice[items[i].Content.SubtypeId] * ratio;
                        m_Logger.WriteLine("Item " + items[i].Content.SubtypeId.String + ": " + m_CachedPrice[items[i].Content.SubtypeId] + " -> " + refundAmount, 4);
                    }
                    _inventory.RemoveItemsAt(i, 1, false);
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
            UpdatePlayerList();
            foreach (IMyPlayer player in m_Players)
            {
                m_Logger.WriteLine(">>   Updating player " + player.SteamUserId, 4);
                if (player.Character == null)
                    continue;
                if (!player.Character.HasInventory)
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
