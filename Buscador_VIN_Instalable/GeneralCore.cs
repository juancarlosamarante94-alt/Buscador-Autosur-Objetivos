namespace AutosurBuscador {
using System;using System.Collections.Generic;using System.Globalization;using System.IO;using System.IO.Compression;using System.Linq;using System.Text.RegularExpressions;using System.Xml.Linq;

public class GeneralBranch {
 public string Key,Dealer,Code,Name;
 public override string ToString(){return Name+" ("+Code+")";}
}
public class GeneralSettings {
 readonly string path;public readonly Dictionary<string,string> Names=new Dictionary<string,string>();
 readonly Dictionary<string,decimal> targets=new Dictionary<string,decimal>(),margins=new Dictionary<string,decimal>();
 public GeneralSettings(string root){Directory.CreateDirectory(root);path=Path.Combine(root,"sucursales.xml");if(!File.Exists(path))return;var xml=XDocument.Load(path).Root;foreach(var e in xml.Elements("Branch"))Names[(string)e.Attribute("key")]=(string)e.Attribute("name");foreach(var e in xml.Elements("Target"))targets[(string)e.Attribute("key")]=Decimal.Parse((string)e.Attribute("value"),CultureInfo.InvariantCulture);foreach(var e in xml.Elements("Margin"))margins[(string)e.Attribute("key")]=Decimal.Parse((string)e.Attribute("value"),CultureInfo.InvariantCulture);}
 static string Month(DateTime p){return p.ToString("yyyy-MM",CultureInfo.InvariantCulture);}
 public decimal Target(DateTime period,string branch){decimal n;return targets.TryGetValue(Month(period)+"|"+branch,out n)?n:0;}
 public void SetTarget(DateTime period,string branch,decimal amount){if(amount<0)throw new Exception("El objetivo no puede ser negativo.");targets[Month(period)+"|"+branch]=amount;}
 public decimal Margin(DateTime period,decimal fallback){decimal n;return margins.TryGetValue(Month(period),out n)?n:fallback;}
 public void SetMargin(DateTime period,decimal amount){if(amount<0||amount>100)throw new Exception("El margen objetivo debe estar entre 0 y 100.");margins[Month(period)]=amount;}
 public void Save(){var xml=new XElement("General",new XAttribute("version",1),Names.OrderBy(x=>x.Key).Select(x=>new XElement("Branch",new XAttribute("key",x.Key),new XAttribute("name",x.Value))),targets.OrderBy(x=>x.Key).Select(x=>new XElement("Target",new XAttribute("key",x.Key),new XAttribute("value",x.Value.ToString(CultureInfo.InvariantCulture)))),margins.OrderBy(x=>x.Key).Select(x=>new XElement("Margin",new XAttribute("key",x.Key),new XAttribute("value",x.Value.ToString(CultureInfo.InvariantCulture)))));string tmp=path+".tmp";new XDocument(xml).Save(tmp);if(File.Exists(path))File.Replace(tmp,path,path+".bak");else File.Move(tmp,path);}
}
public class GeneralResult {
 public string Name,Key;public Metrics Values;public decimal Target,MarginGoal;
 public decimal UtilityTarget{get{return Target*MarginGoal/100;}}
 public decimal Difference{get{return Target-Values.Sale;}}
 public decimal UtilityDifference{get{return UtilityTarget-Values.Profit;}}
 public decimal? Advance{get{return Target==0?(decimal?)null:Values.Sale/Target*100;}}
}
public static class GeneralMath {
 public static string BranchKey(SaleLine line){return line.Dealer+"|"+line.Branch;}
 public static List<GeneralBranch> Branches(GeneralSettings settings,IEnumerable<SaleLine> lines){var keys=settings.Names.Keys.Concat(lines.Select(BranchKey)).Distinct().OrderBy(x=>x).ToList();return keys.Select(k=>{var p=k.Split('|');string name;return new GeneralBranch{Key=k,Dealer=p[0],Code=p.Length>1?p[1]:k,Name=settings.Names.TryGetValue(k,out name)?name:"Sucursal "+(p.Length>1?p[1]:k)};}).ToList();}
 public static GeneralResult Result(string name,string key,IEnumerable<SaleLine> lines,decimal target,decimal margin){return new GeneralResult{Name=name,Key=key,Values=SalesMath.Sum(lines),Target=target,MarginGoal=margin};}
 public static List<GeneralResult> Rows(IEnumerable<SaleLine> source,IEnumerable<GeneralBranch> selected,GeneralSettings settings,DateTime period,decimal margin,bool intern,bool warranty,bool courtesy){var lines=source.Where(x=>SalesMath.Period(x.Date)==period&&SalesMath.Included(x,intern,warranty,courtesy)).ToList();return selected.Select(b=>Result(b.Name,b.Key,lines.Where(x=>BranchKey(x)==b.Key),settings.Target(period,b.Key),margin)).ToList();}
 public static GeneralResult Total(IEnumerable<GeneralResult> rows,IEnumerable<SaleLine> source,IEnumerable<string> selected,DateTime period,decimal margin,bool intern,bool warranty,bool courtesy){var keys=new HashSet<string>(selected);var lines=source.Where(x=>SalesMath.Period(x.Date)==period&&keys.Contains(BranchKey(x))&&SalesMath.Included(x,intern,warranty,courtesy));return Result("TOTAL","",lines,rows.Sum(x=>x.Target),margin);}
}
public class GeneralReport {
 public List<SaleLine> Lines;public Dictionary<string,string> Names=new Dictionary<string,string>();public HashSet<string> Brands=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
 static readonly XNamespace N="http://schemas.openxmlformats.org/spreadsheetml/2006/main";
 static string Value(XElement cell,List<string> shared){if(cell==null)return "";string t=(string)cell.Attribute("t");if(t=="inlineStr")return String.Concat(cell.Descendants(N+"t").Select(x=>x.Value));string v=(string)cell.Element(N+"v")??"";int i;return t=="s"&&Int32.TryParse(v,out i)&&i>=0&&i<shared.Count?shared[i]:v;}
 public static GeneralReport Read(string path,string expectedBrand){var result=new GeneralReport{Lines=SalesXlsx.Read(path)};var branchKeys=new HashSet<string>(result.Lines.Select(GeneralMath.BranchKey));using(var zip=ZipFile.OpenRead(path)){var shared=new List<string>();var entry=zip.GetEntry("xl/sharedStrings.xml");if(entry!=null)using(var f=entry.Open())shared=XDocument.Load(f).Descendants(N+"si").Select(e=>String.Concat(e.Descendants(N+"t").Select(t=>t.Value))).ToList();XDocument book,rels;using(var f=zip.GetEntry("xl/workbook.xml").Open())book=XDocument.Load(f);using(var f=zip.GetEntry("xl/_rels/workbook.xml.rels").Open())rels=XDocument.Load(f);string id=(string)book.Descendants(N+"sheet").First().Attribute(XName.Get("id","http://schemas.openxmlformats.org/officeDocument/2006/relationships"));string target=(string)rels.Root.Elements().First(e=>(string)e.Attribute("Id")==id).Attribute("Target");XDocument data;using(var f=zip.GetEntry(target.StartsWith("/")?target.TrimStart('/'):"xl/"+target).Open())data=XDocument.Load(f);foreach(var row in data.Descendants(N+"row")){var cells=row.Elements(N+"c").ToDictionary(c=>new string(((string)c.Attribute("r")??"").TakeWhile(Char.IsLetter).ToArray()),c=>Value(c,shared).Trim());Func<string,string> get=k=>cells.ContainsKey(k)?cells[k]:"";if(get("K")==""||get("K").Contains("comprobante"))continue;string key=get("A")+"|"+get("B");if(!branchKeys.Contains(key))continue;string brand=get("L");if(brand!="")result.Brands.Add(brand);string label=get("C");if(label!=""){label=Regex.Replace(label,@"^\S+\s*-\s*Taller\s+","",RegexOptions.IgnoreCase);label=Regex.Replace(label,@"\s+(Autosur|Surisan)$","",RegexOptions.IgnoreCase).Trim();result.Names[key]=label;}}}if(result.Brands.Any(b=>!String.Equals(b,expectedBrand,StringComparison.OrdinalIgnoreCase)))throw new Exception("Este panel corresponde a "+expectedBrand+". El reporte contiene otra marca: "+String.Join(", ",result.Brands.ToArray())+".");return result;}
}
}
