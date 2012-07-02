using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace com.zanthra.emw.ViewModels
{
    [Serializable]
    class EveApiAccessException : Exception
    {
        private int _errorCode;
        public int ErrorCode { get { return _errorCode; } }

        private string _errorMessage;
        public string ErrorMessage { get { return _errorMessage; } }

        public EveApiAccessException(XElement errorElement)
        {
            _errorCode = Int32.Parse(errorElement.Attribute("code").Value);
            _errorMessage = errorElement.Value;
        }
    }
}
