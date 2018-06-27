using System;

namespace EntityTableDescriptions.Data
{
	internal class TableColumn : Table
	{
		public TableColumn()
		{

		}
		public TableColumn(Table table) : base(table)
		{
		}

		public TableColumn(TableColumn tableColumn) : base(tableColumn)
		{
			ColumnName = tableColumn.ColumnName;
		}

		public override string ToString() => $"{base.ToString()}.{ColumnName}";

		private bool Equals(TableColumn other) => base.Equals(other) && string.Equals(ColumnName, other.ColumnName, StringComparison.InvariantCultureIgnoreCase);

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj.GetType() == GetType() && Equals((TableColumn)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (base.GetHashCode() * 397) ^ (ColumnName?.ToLowerInvariant().GetHashCode() ?? 0);
			}
		}

		public string ColumnName { get; set; }
	}
}