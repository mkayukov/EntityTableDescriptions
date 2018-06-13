using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using EntityTableDescriptions.Data;
using EntityTableDescriptions.Data.Enum;

namespace EntityTableDescriptions.Services
{
	internal class CurrentDescriptionsService
	{
		private readonly DbContext _context;

		public CurrentDescriptionsService(DbContext context)
		{
			_context = context;
		}

		public Dictionary<Table, DbObjectType> GetDbObjectTypes()
		{
			var tableTypes = _context.Database.SqlQuery<TableType>(@"
				SELECT s.name AS schemaName, t.name AS tableName, 1 AS objectType
				FROM sys.schemas s 
				INNER JOIN sys.tables t ON t.schema_id = s.schema_id
				UNION ALL
				SELECT s.name AS schemaName, v.name AS tableName, 2 AS objectType
				FROM sys.schemas s 
				INNER JOIN sys.views v ON v.schema_id = s.schema_id").ToArray();

			return tableTypes.ToDictionary(x => new Table(x), x => x.ObjectType);
		}
		public Dictionary<Table, string> GetCurrentTableComments()
		{
			var tableComments = _context.Database.SqlQuery<TableComment>(@"
				SELECT s.name AS schemaName, td.objname AS tableName,td.value as Comment
				FROM sys.schemas s
				CROSS APPLY (
					SELECT *
					FROM sys.fn_listextendedproperty('MS_Description','schema',s.name,'table',NULL,NULL,NULL) p
				) td
				UNION ALL
				SELECT s.name AS schemaName, td.objname AS tableName,td.value as Comment
				FROM sys.schemas s
				CROSS APPLY (
					SELECT *
					FROM sys.fn_listextendedproperty('MS_Description','schema',s.name,'view',NULL,NULL,NULL) p
				) td").ToArray();

			return tableComments.ToDictionary(x => new Table(x), x => x.Comment);
		}
		public Dictionary<TableColumn, string> GetCurrentColumnComments()
		{
			var columnComments = _context.Database.SqlQuery<TableColumnComment>(@"
				SELECT s.name AS schemaName, t.name AS tableName, cd.objname AS columnName, cd.value as Comment
				FROM sys.tables t
				INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
				CROSS APPLY (
					SELECT *
					FROM sys.fn_listextendedproperty('MS_Description','schema',s.name,'table',t.name,'column',NULL) p
				) cd
				UNION ALL
				SELECT s.name AS schemaName, t.name AS tableName, cd.objname AS columnName, cd.value as Comment
				FROM sys.views t
				INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
				CROSS APPLY (
					SELECT *
					FROM sys.fn_listextendedproperty('MS_Description','schema',s.name,'view',t.name,'column',NULL) p
				) cd").ToArray();

			return columnComments.ToDictionary(x => new TableColumn(x), x => x.Comment);
		}
	}
}