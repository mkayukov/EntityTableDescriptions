using System.Configuration;

namespace EntityTableDescriptions.Configuration
{
	public sealed class EntityDescriptionsHandler : ConfigurationSection
	{
		public static bool IsEnabledDeploy
		{
			get
			{
				var section = ConfigurationManager.GetSection("entityDescriptions");
				if (section == null) throw new ConfigurationErrorsException(@"Expected configuration section entityDescriptions");
				var entityDescriptionsHandler = section as EntityDescriptionsHandler;
				if (entityDescriptionsHandler == null) throw new ConfigurationErrorsException($"Cannot convert section entityDescriptions to type {typeof(EntityDescriptionsHandler)}");
				return entityDescriptionsHandler.EnabledDeploy;
			}
		}

		internal EntityDescriptionsHandler()
		{
			
		}

		[ConfigurationProperty("enableDeploy", IsRequired = true)]
		internal bool EnabledDeploy
		{
			get { return (bool)this["enableDeploy"]; }
			set { this["enableDeploy"] = value; }
		}
	}
}