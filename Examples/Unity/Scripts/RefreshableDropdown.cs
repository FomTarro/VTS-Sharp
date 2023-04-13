using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VTS.Unity.Examples {
	public abstract class RefreshableDropdown : MonoBehaviour {
		[SerializeField]
		protected Dropdown _dropdown = null;

		// Start is called before the first frame update
		void Start() {
			this._dropdown.onValueChanged.AddListener(SetValue);
		}

		protected abstract void SetValue(int index);

		public abstract void Refresh();

		/// <summary>
		/// Call this in the Refresh implementation when data is returned. This approach allows for asynchronous refreshes.
		/// </summary>
		/// <param name="values"></param>
		protected void RefreshValues(IEnumerable values) {
			string currentSelection =
				this._dropdown.options.Count > 0 ?
				this._dropdown.options[this._dropdown.value].text :
				null;
			List<string> options = new List<string>();
			foreach (object value in values) {
				;
				options.Add(value.ToString());
			}
			this._dropdown.ClearOptions();
			this._dropdown.AddOptions(options);
			this._dropdown.RefreshShownValue();
			// set current selection to the same value as it was before the refresh, if it exists
			int index = Mathf.Min(this._dropdown.options.Count,
				StringToIndex(currentSelection));
			this._dropdown.SetValueWithoutNotify(index);
			// this.SetValue(index);
		}

		protected int StringToIndex(string val) {
			return Mathf.Max(this._dropdown.options.FindIndex((o)
				=> { return o.text.Equals(val); }), 0);
		}
	}
}
