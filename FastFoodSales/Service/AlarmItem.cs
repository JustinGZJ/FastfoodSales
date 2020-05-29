using Stylet;
using System;
using System.Runtime.CompilerServices;

namespace DAQ.Service
{
	public class AlarmItem : PropertyChangedBase
	{
		public string Address
		{
			[CompilerGenerated]
			get
			{
				return this.<Address>k__BackingField;
			}
			[CompilerGenerated]
			set
			{
				if (string.Equals(this.<Address>k__BackingField, value, StringComparison.Ordinal))
				{
					return;
				}
				this.<Address>k__BackingField = value;
				this.NotifyOfPropertyChange("Address");
			}
		}

		public string Content
		{
			[CompilerGenerated]
			get
			{
				return this.<Content>k__BackingField;
			}
			[CompilerGenerated]
			set
			{
				if (string.Equals(this.<Content>k__BackingField, value, StringComparison.Ordinal))
				{
					return;
				}
				this.<Content>k__BackingField = value;
				this.NotifyOfPropertyChange("Content");
			}
		}

		public bool Value
		{
			[CompilerGenerated]
			get
			{
				return this.<Value>k__BackingField;
			}
			[CompilerGenerated]
			set
			{
				if (this.<Value>k__BackingField == value)
				{
					return;
				}
				this.<Value>k__BackingField = value;
				this.NotifyOfPropertyChange("Value");
			}
		}
	}
}
