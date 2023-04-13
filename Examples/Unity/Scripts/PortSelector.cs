using System.Collections.Generic;

namespace VTS.Unity.Examples {
    public class PortSelector : RefreshableDropdown
    {

        private List<string> _portNumbers = new List<string>();
        public ExamplePlugin _plugin = null;
        protected override void SetValue(int index){
            this._plugin.SetPort(int.Parse(this._dropdown.options[index].text));
            this._plugin.Connect();
        }

        public override void Refresh(){
            List<int> sortedPorts = new List<int>(this._plugin.GetPorts().Keys);
            sortedPorts.Sort();
            RefreshValues(sortedPorts);
            // Set display value to actual port
            UpdateDisplay();
        }

        private void UpdateDisplay(){
            int index = StringToIndex(this._plugin.GetPort().ToString());
            if(index > -1 && index < this._dropdown.options.Count){
                this._dropdown.SetValueWithoutNotify(index);
            }
        }

        
    }
}
