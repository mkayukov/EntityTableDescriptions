using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using EntityTableDescriptions.Data;

namespace EntityTableDescriptions.Services
{
	internal class XmlDocParseService
	{
		private readonly XmlNodeList _members;
		private readonly DbContext _context;

		public XmlDocParseService(Assembly assembly, string xmlDocLocation, DbContext context)
		{
			_context = context;
			var doc = new XmlDocument();
			var locationByAssemblyLocation = assembly.Location.Length > 4 
			                                 && new[] {"exe", "dll"}.Contains(assembly.Location.Substring(assembly.Location.Length - 3))
				? assembly.Location.Substring(0, assembly.Location.Length - 3) + "xml"
				: null;
			if (xmlDocLocation != null)
				doc.Load(xmlDocLocation);
			else if (File.Exists($@"{HttpContext.Current.Server.MapPath("~")}bin\{assembly.GetName().Name}.xml"))
				doc.Load($@"{HttpContext.Current.Server.MapPath("~")}bin\{assembly.GetName().Name}.xml");
			else if (File.Exists($@"{HttpContext.Current.Server.MapPath("~")}bin\bin\{assembly.GetName().Name}.xml"))
				doc.Load($@"{HttpContext.Current.Server.MapPath("~")}bin\bin\{assembly.GetName().Name}.xml");
			else if (File.Exists(locationByAssemblyLocation))
				doc.Load(locationByAssemblyLocation);
			else
				return;

			_members = doc.SelectSingleNode("doc")?.SelectSingleNode("members")?.SelectNodes("member");
		}

		public bool HasMembers => _members != null && _members.Count > 0;
		public Dictionary<Table, string> GetTypeDoc(Dictionary<string, Type> types)
		{
			var typeNameRegex = new Regex(@"(?<=^T:).*");
			var nodes = _members.OfType<XmlNode>()
				.Select(x => new
				{
					Name = x.Attributes?.GetNamedItem("name")?.InnerText,
					Comment = x.SelectSingleNode("summary")?.InnerText.Trim()
				})
				.Where(x => x.Name != null && x.Comment != null)
				.Select(x => new { TypeName = typeNameRegex.Match(x.Name).Value, x.Comment })
				.Where(x => types.ContainsKey(x.TypeName))
				.Select(x => new { Type = types[x.TypeName], x.Comment })
				.Select(x => new { Table = GetTableName(x.Type), x.Comment, x.Type })
				.Where(x => x.Table != null)
				.GroupBy(x => x.Table)
				.ToDictionary(x => x.Key, x => x.OrderByDescending(o => o.Type.IsAbstract).First().Comment);
			return nodes;
		}

		public Dictionary<TableColumn, string> GetPropertyDoc(Dictionary<string, Type> types)
		{
			var typeRegex = new Regex(@"(?<=^P:).*(?=\.[\w\d_-]+$)");
			var propRegex = new Regex(@"(?<=^P:.*\.)[\w\d_-]+(?=$)");
			var commentDic = _members.OfType<XmlNode>()
				.Select(x => new
				{
					Name = x.Attributes?.GetNamedItem("name")?.InnerText,
					Comment = x.SelectSingleNode("summary")?.InnerText.Trim()
				})
				.Where(x => x.Name != null && x.Comment != null)
				.Select(x => new
				{
					Type = string.IsNullOrEmpty(typeRegex.Match(x.Name).Value) ? null : _context.GetType().Assembly.GetType(typeRegex.Match(x.Name).Value),
					x.Comment,
					PropName = propRegex.Match(x.Name).Value,
				})
				.Where(x => x.Type != null)
				.Select(x => new { x.Type, x.PropName, PropType = x.Type.GetProperty(x.PropName)?.PropertyType, x.Comment })
				.Where(x => x.PropType != null && (x.PropType.IsValueType || x.PropType == typeof(string)))
				.Select(x => new { x.Type, x.Comment, x.PropName })
				.GroupBy(x => x.Type)
				.ToDictionary(x => x.Key, x => x.Select(p => new { p.PropName, p.Comment }).ToList());

			var tables = types
				.Select(x => new { Table = GetTableName(x.Value), Type = x.Value })
				.Where(x => x.Table != null)
				.Select(x =>
				{
					var order = 0;
					var tableComments = commentDic.ContainsKey(x.Type) ? commentDic[x.Type].Select(c => new { c.PropName, c.Comment, Order = order++ }).ToList() : null;
					foreach (var baseClass in GetBaseClasses(x.Type))
					{
						var baseComments = commentDic.ContainsKey(baseClass.Value) ? commentDic[baseClass.Value].Select(c => new { c.PropName, c.Comment, Order = order++ }) : null;
						if (tableComments == null)
							tableComments = baseComments?.ToList();
						else if (baseComments != null)
							tableComments.AddRange(baseComments.Where(b => tableComments.Select(t => t.PropName).Contains(b.PropName) == false));
					}
					return tableComments?.Select(t => new { x.Table, x.Type, t.PropName, t.Comment, t.Order });
				})
				.Where(x => x != null)
				.SelectMany(x => x)
				.GroupBy(x => new TableColumn(x.Table) { ColumnName = x.PropName })
				.ToDictionary(x => x.Key, x => x.OrderBy(o => o.Order).First().Comment);
			return tables;
		}
		private SortedList<int, Type> GetBaseClasses(Type type)
		{
			var list = new SortedList<int, Type>();
			SetChildType(list, type, 0);
			return list;
		}
		private void SetChildType(SortedList<int, Type> list, Type parent, int order)
		{
			if (parent.BaseType == null) return;
			list.Add(order, parent.BaseType);
			SetChildType(list, parent.BaseType, ++order);
		}
		private Table GetTableName(Type tableType)
		{
			var sql = _context.Set(tableType).Sql; // Вбросит ошибку, если эта сущность не является частью модели
			var regex = new Regex("FROM (?<table>.*) AS");
			var match = regex.Match(sql);
			var fullTableName = match.Groups["table"].Value;
			var parseRegex = new Regex(@"(\[(?<schema>\w+)\]\.)?\[(?<table>.*)\]");
			var parseMatch = parseRegex.Match(fullTableName);
			if (!match.Success || !parseMatch.Success) return null;
			return new Table
			{
				SchemaName = parseMatch.Groups["schema"].Value,
				TableName = parseMatch.Groups["table"].Value
			};
		}
	}
}