using System.Collections.Generic;
using UnityEngine;
using VTS.Core;

namespace VTS.Unity.Examples {
    public class ExpressionSelector : RefreshableDropdown
    {
        public ExamplePlugin _plugin = null;
        private string _expression = "";
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
            Debug.Log(this._expression);
        }

        public void ToggleExpression(bool state){
            this._plugin.SetExpressionState(
                this._expression,
                state,
                (s) => {},
                (r) => {}
            );
        }
    }
}
