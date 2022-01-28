// ;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;
using VRageMath;

namespace PocketShieldCore
{
    static class Utils
    {
        public static float MyPow(float _base, uint _exponent)
        {
            if (_exponent == 1)
                return _base;

            if (_exponent > 50)
                return (float)Math.Pow(_base, _exponent);

            float res = _base;
            for (uint i = 0; i < _exponent - 1; ++i)
                res *= _base;

            return res;
        }

        public static float MyPowWithLog(float _base, uint _exponent, ExShared.Logger _logger)
        {
            if (_exponent == 1)
                return _base;

            if (_exponent > 50)
                return (float)Math.Pow(_base, _exponent);

            float res = _base;
            for (uint i = 0; i < _exponent - 1; ++i)
            {
                string log = "base = " + res;
                res *= _base;
                log += " -> " + res;
                _logger.WriteLine(log);
            }

            return res;
        }

        public static string FormatShieldValue(float _value)
        {
            if ((int)_value > 10000)
                return string.Format("{0:F1}k", _value / 1000.0f);

            return ((int)_value).ToString();
        }

        public static string FormatPercent(float _percent)
        {
            if (_percent == 0.0f)
                return "0%";
            if (_percent == 1.0f)
                return "100%";
            if (_percent < 0.001f)
                return "0.1%";
            if (_percent > 0.999)
                return "99.9%";

            return string.Format("{0:F1}%", _percent * 100.0f);
        }

        public static Color CalculateBGColor(Color _color, float _opacity)
        {
            // SK: Stolen stuff
            // https://github.com/THDigi/BuildInfo/blob/master/Data/Scripts/BuildInfo/Utilities/Utils.cs#L256-L263
            //_color *= _opacity * _opacity * 1.075f;
            _color *= _opacity * _opacity * 0.90f;
            _color.A = (byte)(_opacity * 255.0f);

            return _color;
        }

        public static string GetCharacterName(IMyCharacter _character)
        {
            if (_character == null)
                return "null";
            if (_character.DisplayName != string.Empty)
                return _character.DisplayName;
            return _character.Name;
        }
    }

}
