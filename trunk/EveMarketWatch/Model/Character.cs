using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace com.zanthra.emw.Model
{
    [Serializable]
    class Character
    {
        public long characterID { get; set; }
        public string name { get; set; }
        public string race { get; set; }
        public string bloodLine { get; set; }
        public bool male { get; set; }
        public string corporationName { get; set; }
        public long corporationID { get; set; }
        public double balance { get; set; }

        public string imageUrl
        {
            get { return String.Format("http://image.eveonline.com/Character/{0}_128.jpg", characterID); }
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;

            if (this.GetType() != obj.GetType()) return false;

            // safe because of the GetType check
            Character character = (Character)obj;

            return Object.Equals(characterID, character.characterID);
        }

        public override int GetHashCode()
        {
            return characterID.GetHashCode();
        }
    }
}
