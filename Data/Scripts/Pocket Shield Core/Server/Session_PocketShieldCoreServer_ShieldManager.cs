// ;
using ExShared;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace PocketShieldCore
{
    public partial class Session_PocketShieldCoreServer
    {
        private Dictionary<long, CharacterShieldManager> m_ShieldManager_CharacterShieldManagers = null;
        private Dictionary<MyStringHash, List<object>> m_ShieldManager_EmitterConstructionData = null;
        private Dictionary<long, MyTuple<Logger, Logger>> m_ShieldManager_ShieldLoggers = null;

        private PocketShieldAPIV2.ShieldEmitterProperties m_CachedProps = null;

        private ShieldEmitter ShieldManager_CreateEmitter(IMyCharacter _character, MyStringHash _subtypeId)
        {
            if (!m_ShieldManager_EmitterConstructionData.ContainsKey(_subtypeId))
            {
                m_Logger.WriteLine("Emitter " + _subtypeId.String + " is not registered", 2);
                return null;
            }

            PocketShieldAPIV2.ShieldEmitterProperties prop = new PocketShieldAPIV2.ShieldEmitterProperties(m_ShieldManager_EmitterConstructionData[_subtypeId]);
            Logger logger;
            if (prop.IsManual)
                logger = ShieldManager_GetManualShieldLogger(_character);
            else
                logger = ShieldManager_GetAutoShieldLogger(_character);

            return new ShieldEmitter(prop, _character, logger);
        }

        private ShieldEmitter ShieldManager_AddNewEmitter(IMyCharacter _character, MyStringHash _subtypeId, bool _isManualEmitter)
        {
            m_Logger.WriteLine("> Adding new Emitter " + _subtypeId.String + "..", 4);
            ShieldEmitter emitter = ShieldManager_CreateEmitter(_character, _subtypeId);
            if (emitter == null)
                return null;

            if (!m_ShieldManager_CharacterShieldManagers.ContainsKey(_character.EntityId))
                m_ShieldManager_CharacterShieldManagers[_character.EntityId] = new CharacterShieldManager(_character) { m_Logger = m_Logger };

            if (_isManualEmitter)
                m_ShieldManager_CharacterShieldManagers[_character.EntityId].ManualEmitter = emitter;
            else
                m_ShieldManager_CharacterShieldManagers[_character.EntityId].AutoEmitter = emitter;

            //if (emitter.IsManual)
            //    emitter.IsActive = false;

            return emitter;
        }

        private ShieldEmitter ShieldManager_ReplaceEmitter(IMyCharacter _character, MyStringHash _subtypeId, bool _isManualEmitter)
        {
            m_Logger.WriteLine("> Replacing with Emitter " + _subtypeId.String + "..", 4);
            ShieldEmitter emitter = ShieldManager_CreateEmitter(_character, _subtypeId);
            if (emitter == null)
                return null;

            if (emitter.IsManual != _isManualEmitter)
            {
                m_Logger.WriteLine("Emitter \"Manual\" properties does not match: Emitter is " + emitter.IsManual + ", parameter is " + _isManualEmitter, 1);
                return null;
            }
            
            if (_isManualEmitter)
            {
                m_ShieldManager_CharacterShieldManagers[_character.EntityId].ManualEmitter = emitter;
                Logger logger = ShieldManager_GetManualShieldLogger(_character);
                logger.WriteLine("=====> New Emitter <===== ");
                logger.WriteLine("  " + _subtypeId.String);
                logger.WriteLine("========================= ");
            }
            else
            {
                m_ShieldManager_CharacterShieldManagers[_character.EntityId].AutoEmitter = emitter;
                Logger logger = ShieldManager_GetAutoShieldLogger(_character);
                logger.WriteLine("=====> New Emitter <===== ");
                logger.WriteLine("  " + _subtypeId.String);
                logger.WriteLine("========================= ");
            }

            return emitter;
        }

        private void ShieldManager_DropEmitter(IMyCharacter _character, bool _isManualEmitter)
        {
            if (!m_ShieldManager_CharacterShieldManagers.ContainsKey(_character.EntityId))
                return;

            m_Logger.WriteLine("> Dropping Emitter..", 4);
            if (_isManualEmitter)
            {
                if (m_ShieldManager_CharacterShieldManagers[_character.EntityId].ManualEmitter != null)
                {
                    m_ShieldManager_CharacterShieldManagers[_character.EntityId].ManualEmitter = null;
                    m_ShieldManager_ShieldLoggers[_character.EntityId].Item1.Close();
                }
            }
            else
            {
                if (m_ShieldManager_CharacterShieldManagers[_character.EntityId].AutoEmitter != null)
                {
                    m_ShieldManager_CharacterShieldManagers[_character.EntityId].AutoEmitter = null;
                    m_ShieldManager_ShieldLoggers[_character.EntityId].Item2.Close();
                }
            }

            if (!m_ShieldManager_CharacterShieldManagers[_character.EntityId].HasAnyEmitter)
            {
                m_ShieldManager_CharacterShieldManagers[_character.EntityId].Close();
                m_ShieldManager_CharacterShieldManagers.Remove(_character.EntityId);
                m_ShieldManager_ShieldLoggers.Remove(_character.EntityId);
                m_SaveData.ForceRemoveEntry(_character.EntityId);
            }

            m_ForceSyncPlayers.Add(GetPlayerSteamUid(_character));
        }

        private void ShieldManager_ActivateManualShield(long _entityId, bool _activate)
        {
            m_Logger.Write("> Character [" + _entityId + "] is trying to " + (_activate ? "activate" : "deactivate") + " Manual Emitter..", 2);

            if (m_ShieldManager_CharacterShieldManagers.ContainsKey(_entityId) &&
                m_ShieldManager_CharacterShieldManagers[_entityId].ManualEmitter != null)
            {
                m_ShieldManager_CharacterShieldManagers[_entityId].ManualEmitter.IsActive = _activate;
                m_Logger.WriteInline(" success\n", 2);
            }
            else
            {
                m_Logger.WriteInline(" failed (character doesn't have Manual Emitter\n", 2);
            }
        }

        private void ShieldManager_TurnShieldOnOff(long _entityId, bool _activate, bool _isManual)
        {
            m_Logger.Write("> Character [" + _entityId + "] is trying to turn " + (_activate ? "on" : "off") + " " + (_isManual ? "Manual" : "Auto") + " Emitter..", 2);

            if (!m_ShieldManager_CharacterShieldManagers.ContainsKey(_entityId))
            {
                m_Logger.WriteInline(" failed (character doesn't have any Emitter\n", 2);
                return;
            }

            if (_isManual && m_ShieldManager_CharacterShieldManagers[_entityId].ManualEmitter != null)
            {
                m_ShieldManager_CharacterShieldManagers[_entityId].ManualEmitter.IsTurnedOn = _activate;
                m_Logger.WriteInline(" success\n", 2);
            }
            else
            {
                m_Logger.WriteInline(" failed (character doesn't have Manual Emitter\n", 2);
            }

            if (_isManual && m_ShieldManager_CharacterShieldManagers[_entityId].ManualEmitter != null)
            {
                m_ShieldManager_CharacterShieldManagers[_entityId].AutoEmitter.IsTurnedOn = _activate;
                m_Logger.WriteInline(" success\n", 2);
            }
            else
            {
                m_Logger.WriteInline(" failed (character doesn't have Auto Emitter\n", 2);
            }
        }

        private Logger ShieldManager_GetManualShieldLogger(IMyCharacter _character)
        {
            if (!m_ShieldManager_ShieldLoggers.ContainsKey(_character.EntityId))
                m_ShieldManager_ShieldLoggers[_character.EntityId] = new MyTuple<Logger, Logger>();

            var loggers = m_ShieldManager_ShieldLoggers[_character.EntityId];
            if (loggers.Item1 == null)
            {
                string loggerName = "shield_" + _character.EntityId;
                if (!_character.IsBot)
                    loggerName += "_" + Utils.GetCharacterName(_character);
                loggerName += "_Manual";

                loggers.Item1 = new Logger(loggerName)
                {
                    LogLevel = m_Config.LogLevel,
                    Suppressed = m_Config.SuppressAllShieldLog
                };
                m_ShieldManager_ShieldLoggers[_character.EntityId] = loggers;
            }

            return loggers.Item1;
        }

        private Logger ShieldManager_GetAutoShieldLogger(IMyCharacter _character)
        {
            if (!m_ShieldManager_ShieldLoggers.ContainsKey(_character.EntityId))
                m_ShieldManager_ShieldLoggers[_character.EntityId] = new MyTuple<Logger, Logger>();

            var loggers = m_ShieldManager_ShieldLoggers[_character.EntityId];
            if (loggers.Item2 == null)
            {
                string loggerName = "shield_" + _character.EntityId;
                if (!_character.IsBot)
                    loggerName += "_" + Utils.GetCharacterName(_character);
                loggerName += "_Auto";

                loggers.Item2 = new Logger(loggerName)
                {
                    LogLevel = m_Config.LogLevel,
                    Suppressed = m_Config.SuppressAllShieldLog
                };
                m_ShieldManager_ShieldLoggers[_character.EntityId] = loggers;
            }

            return loggers.Item2;
        }
        

    }

    public class CharacterShieldManager
    {
        public bool HasAnyEmitter { get { return ManualEmitter != null || AutoEmitter != null; } }
        public bool RequireSync
        {
            get
            {
                if (ManualEmitter != null && ManualEmitter.RequireSync)
                    return true;
                if (AutoEmitter != null && AutoEmitter.RequireSync)
                    return true;

                return false;
            }
        }
        public Vector3 Position { get { return Character.WorldVolume.Center; } }


        public IMyCharacter Character { get; private set; } = null;
        internal PSProjectileDetector ProjectileDetector { get; private set; } = null;
        
        public float LastAutoEnergy { get; private set; } = 0.0f;
        public bool LastAutoTurnedOn { get; private set; } = false;

        public ShieldEmitter ManualEmitter = null;
        public ShieldEmitter AutoEmitter = null;


        public int ManualEmitterIndex = -1;
        public int AutoEmitterIndex = -1;

        public Logger m_Logger = null;

        public CharacterShieldManager(IMyCharacter _character)
        {
            Character = _character;

            ProjectileDetector = new PSProjectileDetector(this, (float)_character.WorldVolume.Radius);
            ProjectileDetector.m_Logger = m_Logger;
            //MyAPIGateway.Projectiles.AddHitDetector(ProjectileDetector);
        }

        public void Close()
        {
            //MyAPIGateway.Projectiles.RemoveHitDetector(ProjectileDetector);
            


        }




        public void Update(int _skipTicks)
        {
            if (ManualEmitter != null)
            {
                ManualEmitter.Update(_skipTicks);
            }

            if (AutoEmitter != null)
            {
                AutoEmitter.Update(_skipTicks);
                LastAutoEnergy = AutoEmitter.Energy;
                LastAutoTurnedOn = AutoEmitter.IsTurnedOn;
            }


            ProjectileDetector.Update();



        }

    }

}
