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

        private void ShieldFactory_ReplaceEmitter(IMyCharacter _character, ShieldEmitter _newEmitter)
        {
            if (_newEmitter == null)
            {
                m_ShieldEmitters.Remove(_character.EntityId);
                RemoveShieldLogger((ulong)_character.EntityId);
            }
            else
            {
                m_ShieldEmitters[_character.EntityId] = _newEmitter;
                Logger logger = GetLogger((ulong)_character.EntityId);
                logger.WriteLine(" > New Emitter < " + _newEmitter.SubtypeId.String);
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
