using System;

namespace VTS {
	public static class EnumUtils {
		public static TAttribute GetAttribute<TAttribute>(this Enum value) where TAttribute : Attribute {
			Type enumType = value.GetType();
			string name = Enum.GetName(enumType, value);
			TAttribute[] attributes = (TAttribute[])enumType.GetField(name).GetCustomAttributes(typeof(TAttribute), false);
			return attributes.Length > 0 ? attributes[0] : null;
		}
	}
}