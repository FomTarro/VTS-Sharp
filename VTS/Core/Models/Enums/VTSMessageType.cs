using System;

namespace VTS {
	/// <summary>
	/// 
	/// </summary>
	public enum VTSMessageType {
        [StringValues("APIError", typeof(VTSErrorData))]
		APIError,
        [StringValues("APIState", typeof(VTSStateData))]
		APIState,
		[StringValues("AuthenticationToken", typeof(VTSAuthData))]
		AuthenticationToken,

	}

	public class StringValues : Attribute {
		public string Request { get; private set; }
		public string Response { get; private set; }
        public Type ResponseType { get; private set; }
		public StringValues(string name, Type responseType) {
			this.Request = name + "Request";
            this.Response = name + "Response";
            this.ResponseType = responseType;
		}
	}
}