using System.Collections.Generic;

namespace VTS.Examples {
    public class PortSelector : RefreshableDropdown
    {

        private List<string> _portNumbers = new List<string>();
        public ExamplePlugin _plugin = null;
        protected override void SetValue(int index){
            this._plugin.SetPort(int.Parse(this._dropdown.options[index].text));
            this._plugin.Connect();
        }

        public override void Refresh()
        {
            RefreshValues(this._plugin.GetPorts().Keys);
        }
    }
}
