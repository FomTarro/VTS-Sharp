using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VTS.Models;

namespace VTS.Examples {
    public class ExpressionSelector : RefreshableDropdown
    {
        public ExamplePlugin _plugin = null;
        private string _expression = "";
        private bool _state = true;
        public override void Refresh()
        {
            List<string> expressionFiles = new List<string>();
            this._plugin.GetExpressionStateList(
                (s) => {
                    foreach(ExpressionData expression in s.data.expressions){
                        expressionFiles.Add(expression.file);
                    }
                    this.RefreshValues(expressionFiles);
                },
                (r) => {}
            );
        }

        protected override void SetValue(int index)
        {
            this._expression = this._dropdown.options[index].text;
        }

        public void ToggleExpression(){
            Refresh();
            this._plugin.SetExpressionState(
                this._expression,
                this._state,
                (s) => {},
                (r) => {}
            );
            this._state = !this._state;
        }
    }
}
