using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Xml;
using EntityTableDescriptions.Data;
using EntityTableDescriptions.Services;
using Tools;

namespace EntityTableDescriptions
{
	public static class DbDescriptionUpdater
	{
		public static void UpdateDescriptions<TContext>(TContext context, IEnumerable<Type> entityTypes = null, string xmlDocLocation = null) where TContext : DbContext
		{
			var assembly = typeof(TContext).Assembly;
			var parser = new XmlDocParseService(assembly,xmlDocLocation,context);
			if (!parser.HasMembers) return;
			using (var tran = context.Database.BeginTransaction())
			{
				var currentDescriptions = new CurrentDescriptionsService(context);
				var typeDictionary = (entityTypes ?? assembly.GetTypes().Where(x => x.IsClass)).Where(x => IsEntityType(context, x)).ToDictionary(x => x.FullName);
				var typeDoc = parser.GetTypeDoc(typeDictionary);
				var propertyDoc = parser.GetPropertyDoc(typeDictionary);
				var tableDoc = currentDescriptions.GetCurrentTableComments();
				var columnDoc = currentDescriptions.GetCurrentColumnComments();
				var objectTypes = currentDescriptions.GetDbObjectTypes();

				var addTableComments = typeDoc.Where(x => objectTypes.ContainsKey(x.Key) && tableDoc.ContainsKey(x.Key) == false).ToArray();
				var updateTableComments = typeDoc.Where(x => objectTypes.ContainsKey(x.Key)
					&& tableDoc.ContainsKey(x.Key)
					&& tableDoc[x.Key] != x.Value).ToArray();
				var dropTableComments = tableDoc.Where(x => objectTypes.ContainsKey(x.Key) && typeDoc.ContainsKey(x.Key) == false).ToArray();

				var addColumnComments = propertyDoc
					.Where(x => objectTypes.ContainsKey(new Table(x.Key)) && columnDoc.ContainsKey(x.Key) == false)
					.ToArray();
				var updateColumnComments = propertyDoc
					.Where(x => objectTypes.ContainsKey(new Table(x.Key)) && columnDoc.ContainsKey(x.Key) && columnDoc[x.Key] != x.Value)
					.ToArray();
				var dropColumnComments = columnDoc
					.Where(x => objectTypes.ContainsKey(new Table(x.Key)) && propertyDoc.ContainsKey(x.Key) == false)
					.ToArray();

				var modifyService = new ModifyDescriptionsService(context);
				foreach (var comment in addTableComments)
					modifyService.AddTableDescription(comment.Key, comment.Value, objectTypes[comment.Key]);
				foreach (var comment in updateTableComments)
					modifyService.UpdateTableDescription(comment.Key, comment.Value, objectTypes[comment.Key]);
				foreach (var comment in dropTableComments)
					modifyService.RemoveTableDescription(comment.Key, objectTypes[comment.Key]);

				foreach (var comment in addColumnComments)
					modifyService.AddColumnDescription(comment.Key, comment.Value, objectTypes[new Table(comment.Key)]);
				foreach (var comment in updateColumnComments)
					modifyService.UpdateColumnDescription(comment.Key, comment.Value, objectTypes[new Table(comment.Key)]);
				foreach (var comment in dropColumnComments)
					modifyService.RemoveColumnDescription(comment.Key, objectTypes[new Table(comment.Key)]);

				tran.Commit();
			}
		}
		private static bool IsEntityType(DbContext context, Type type)
		{
			try
			{
				return context.Set(type).Sql != null;
			}
			catch
			{
				return false;
			}
		}
	}
}