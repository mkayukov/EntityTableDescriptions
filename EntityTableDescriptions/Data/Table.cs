namespace EntityTableDescriptions.Data
{
	internal class Table
	{
		public Table()
		{

		}
		public Table(Table table)
		{
			SchemaName = table.SchemaName;
			TableName = table.TableName;
		}

		public override string ToString() => $"{SchemaName}.{TableName}";

		protected bool Equals(Table other)
		{
			return string.Equals(SchemaName, other.SchemaName) && string.Equals(TableName, other.TableName);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj.GetType() == GetType() && Equals((Table)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = SchemaName?.GetHashCode() ?? 0;
				hashCode = (hashCode * 397) ^ (TableName?.GetHashCode() ?? 0);
				return hashCode;
			}
		}

		public string SchemaName { get; set; }
		public string TableName { get; set; }
	}
}