using System.Data.Entity;
using System.Data.SqlClient;
using EntityTableDescriptions.Data;
using EntityTableDescriptions.Data.Enum;

namespace EntityTableDescriptions.Services
{
	internal class ModifyDescriptionsService
	{
		private readonly DbContext _context;

		public ModifyDescriptionsService(DbContext context)
		{
			_context = context;
		}

		public void AddColumnDescription(TableColumn tableColumn, string description, DbObjectType objectType)
			=> RunSql($@"EXEC sp_addextendedproperty  
						@name = N'MS_Description', @value = @desc,
						@level0type = N'Schema', @level0name = @schema,
						@level1type = N'{objectType}',  @level1name = @table,
						@level2type = N'Column', @level2name = @column",
				new SqlParameter("@schema", tableColumn.SchemaName),
				new SqlParameter("@table", tableColumn.TableName),
				new SqlParameter("@desc", description),
				new SqlParameter("@column", tableColumn.ColumnName));

		public void UpdateColumnDescription(TableColumn tableColumn, string description, DbObjectType objectType)
			=> RunSql($@"EXEC sp_updateextendedproperty
				@name = N'MS_Description', @value = @desc,
				@level0type = N'Schema', @level0name = @schema,
				@level1type = N'{objectType}',  @level1name = @table,
				@level2type = N'Column', @level2name = @column",
				new SqlParameter("@schema", tableColumn.SchemaName),
				new SqlParameter("@table", tableColumn.TableName),
				new SqlParameter("@desc", description),
				new SqlParameter("@column", tableColumn.ColumnName));

		public void RemoveColumnDescription(TableColumn tableColumn, DbObjectType objectType)
			=> RunSql($@"EXEC sys.sp_dropextendedproperty 
				@name = N'MS_Description',
				@level0type = N'Schema', @level0name = @schema,
				@level1type = N'{objectType}', @level1name = @table,
				@level2type = N'Column', @level2name = @column",
				new SqlParameter("@schema", tableColumn.SchemaName),
				new SqlParameter("@table", tableColumn.TableName),
				new SqlParameter("@column", tableColumn.ColumnName));

		public void AddTableDescription(Table table, string description, DbObjectType objectType)
			=> RunSql($@"EXEC sp_addextendedproperty
				@name = N'MS_Description', @value = @desc,
				@level0type = N'Schema', @level0name = @schema,
				@level1type = N'{objectType}',  @level1name = @table",
				new SqlParameter("@schema", table.SchemaName),
				new SqlParameter("@table", table.TableName),
				new SqlParameter("@desc", description));

		public void UpdateTableDescription(Table table, string description, DbObjectType objectType)
			=> RunSql($@"EXEC sp_updateextendedproperty
				@name = N'MS_Description', @value = @desc,
				@level0type = N'Schema', @level0name = @schema,
				@level1type = N'{objectType}',  @level1name = @table",
				new SqlParameter("@schema", table.SchemaName),
				new SqlParameter("@table", table.TableName),
				new SqlParameter("@desc", description));

		public void RemoveTableDescription(Table table, DbObjectType objectType)
			=> RunSql($@"EXEC sys.sp_dropextendedproperty 
					@name = N'MS_Description',
					@level0type = N'Schema', @level0name = @schema,
					@level1type = N'{objectType}', @level1name = @table",
				new SqlParameter("@schema", table.SchemaName),
				new SqlParameter("@table", table.TableName));

		private void RunSql(string cmdText, params SqlParameter[] parameters)
		{
			var cmd = _context.Database.Connection.CreateCommand();
			cmd.CommandText = cmdText;
			cmd.Transaction = _context.Database.CurrentTransaction.UnderlyingTransaction;
			foreach (var p in parameters)
				cmd.Parameters.Add(p);
			cmd.ExecuteNonQuery();
		}
	}
}