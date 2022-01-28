// ;
using ExShared;
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace PocketShieldCore
{
    public partial class Session_PocketShieldCoreServer
    {
        private ShieldEmitter m_FirstEmitterFound = null;

        private bool ShieldFactory_TryCreateEmitter(MyStringHash _subtypeId, IMyCharacter _character)
        {
            //m_Logger.WriteLine("    TryCreateEmitter called");
            //m_Logger.WriteLine("    callback count: " + m_ApiBackend_EmitterPropCallbacks.Count);
            foreach (var callback in m_ApiBackend_EmitterPropCallbacks)
            {
                IList<object> data = (callback as Func<MyStringHash, IList<object>>).Invoke(_subtypeId);
                //m_Logger.WriteLine("      data is null? " + (data == null));
                if (data == null)
                    continue;

                PocketShieldAPI.ShieldEmitterProperties prop = new PocketShieldAPI.ShieldEmitterProperties(data);
                m_FirstEmitterFound = new ShieldEmitter(prop, _character, GetLogger((ulong)_character.EntityId));
                if (m_FirstEmitterFound != null)
                    return true;
            }

            return false;
        }

        private void ShieldFactory_UpdateEmittersOnceBeforeSim()
        {
            m_Logger.WriteLine("  Updating all Emitters once before sim..", 1);
            foreach (long key in m_PlayerShieldEmitters.Keys)
            {
                float value = m_SaveData.GetPlayerData(key);
                m_PlayerShieldEmitters[key].Energy = value;
            }

            foreach (long key in m_NpcShieldEmitters.Keys)
            {
                float value = m_SaveData.GetNpcData(key);
                m_NpcShieldEmitters[key].Energy = value;
            }
        }

        private void ShieldFactory_UpdateEmitters(int _ticks)
        {
            foreach (long key in m_PlayerShieldEmitters.Keys)
            {
                m_PlayerShieldEmitters[key].Update(_ticks);
            }

            foreach (long key in m_NpcShieldEmitters.Keys)
            {
                m_NpcShieldEmitters[key].Update(_ticks);
            }
        }

        private ShieldEmitter ShieldFactory_GetEmitter(IMyCharacter _character)
        {
            if (GetPlayerSteamUid(_character) == 0U)
            {
                if (m_NpcShieldEmitters.ContainsKey(_character.EntityId))
                    return m_NpcShieldEmitters[_character.EntityId];
            }
            else
            {
                long playerId = (long)GetPlayerSteamUid(_character);
                if (m_PlayerShieldEmitters.ContainsKey(playerId))
                    return m_PlayerShieldEmitters[playerId];
            }

            return null;
        }

        private void ShieldFactory_ReplaceEmitter(IMyCharacter _character, ShieldEmitter _newEmitter)
        {
            if (_character == null)
                return;
            
            if (GetPlayerSteamUid(_character) == 0U)
            {
                if (_newEmitter == null)
                {
                    m_NpcShieldEmitters.Remove(_character.EntityId);
                    RemoveShieldLogger((ulong)_character.EntityId);
                }
                else
                {
                    m_NpcShieldEmitters[_character.EntityId] = _newEmitter;
                    Logger logger = GetLogger((ulong)_character.EntityId);
                    logger.WriteLine(" > New Emitter < " + _newEmitter.SubtypeId.String);
                }
            }
            else
            {
                long playerId = (long)GetPlayerSteamUid(_character);
                if (_newEmitter == null)
                {
                    m_PlayerShieldEmitters.Remove(playerId);
                    RemoveShieldLogger((ulong)_character.EntityId);
                }
                else
                {
                    m_PlayerShieldEmitters[playerId] = _newEmitter;
                    Logger logger = GetLogger((ulong)_character.EntityId);
                    logger.WriteLine(" > New Emitter < " + _newEmitter.SubtypeId.String);
                }
            }
        }

        private void RemoveShieldLogger(ulong _id)
        {
            if (m_ShieldLoggers.ContainsKey(_id))
            {
                m_ShieldLoggers[_id].Close();
                m_ShieldLoggers.Remove(_id);
            }
        }

        public Logger GetLogger(ulong _id)
        {
            if (!m_ShieldLoggers.ContainsKey(_id))
                m_ShieldLoggers[_id] = new Logger("shield_" + _id)
                {
                    LogLevel = m_Config.LogLevel,
                    Suppressed = m_Config.SuppressAllShieldLog
                };

            return m_ShieldLoggers[_id];
        }


    }
}
