namespace AutosurBuscador {
using System; using System.Collections.Generic; using System.Globalization; using System.IO; using System.IO.Compression; using System.Linq; using System.Xml.Linq;
public class SaleLine {
 public DateTime Date; public string Dealer="",Branch="",Type="",Code="",Letter="",Number="",Sector="",Part=""; public decimal Sale,Profit;
 public string Key {get{return Dealer+"|"+Branch+"|"+Code+"|"+Letter+"|"+Number;}}
 public string Category {get {string s=Type.ToUpperInvariant();return s.Contains("CORTES")?"Cortesía":s.Contains("GARANT")?"Garantía":s.Contains("INTERNA")?"Interno":"Comercial";}}
 public bool IsTicket {get{return !Type.ToUpperInvariant().Contains("CREDITO")&&!Type.ToUpperInvariant().Contains("DEBITO");}}
}
public class Goal {public decimal Total,Taller=40,Margin=22;}
public class Snapshot {public DateTime Period,Cutoff,Loaded;public string Source="";public List<SaleLine> Lines=new List<SaleLine>();}
public class Metrics {
 public decimal Sale,Profit;public int Tickets;
 public decimal? Margin {get{return Sale==0?(decimal?)null:Profit/Sale*100;}}
 public decimal? Average {get{return Tickets==0?(decimal?)null:Sale/Tickets;}}
}
public static class SalesMath {
 public static DateTime Period(DateTime date){var p=new DateTime(date.Year,date.Month,1);return date.Day>=27?p.AddMonths(1):p;}
 public static DateTime Start(DateTime p){return new DateTime(p.Year,p.Month,27).AddMonths(-1);}
 public static DateTime End(DateTime p){return new DateTime(p.Year,p.Month,26);}
 public static Metrics Sum(IEnumerable<SaleLine> lines){var a=lines.ToList();return new Metrics{Sale=a.Sum(x=>x.Sale),Profit=a.Sum(x=>x.Profit),Tickets=a.Where(x=>x.IsTicket).Select(x=>x.Key).Distinct().Count()};}
 public static bool Included(SaleLine line,bool intern,bool warranty,bool courtesy){return line.Category=="Interno"?intern:line.Category=="Garantía"?warranty:line.Category=="Cortesía"?courtesy:true;}
 public static int Workdays(DateTime start,DateTime end,HashSet<DateTime> holidays){int n=0;for(var d=start.Date;d<=end.Date;d=d.AddDays(1))if(d.DayOfWeek!=DayOfWeek.Saturday&&d.DayOfWeek!=DayOfWeek.Sunday&&!holidays.Contains(d))n++;return n;}
 public static DateTime EquivalentCutoff(DateTime period,int days,HashSet<DateTime> holidays){var start=Start(period);for(var d=start;d<=End(period);d=d.AddDays(1))if(Workdays(start,d,holidays)>=days)return d;return End(period);}
}
public static class SalesXlsx {
 static readonly XNamespace N="http://schemas.openxmlformats.org/spreadsheetml/2006/main";
 static string Cell(XElement c,List<string> strings){string type=(string)c.Attribute("t");if(type=="inlineStr")return String.Concat(c.Descendants(N+"t").Select(x=>x.Value));string v=(string)c.Element(N+"v")??"";int index;if(type=="s"&&Int32.TryParse(v,out index)&&index>=0&&index<strings.Count)return strings[index];return v;}
 public static List<SaleLine> Read(string path){using(var zip=ZipFile.OpenRead(path)){
 var strings=new List<string>();var shared=zip.GetEntry("xl/sharedStrings.xml");if(shared!=null)using(var f=shared.Open())strings=XDocument.Load(f).Descendants(N+"si").Select(x=>String.Concat(x.Descendants(N+"t").Select(t=>t.Value))).ToList();
 XDocument book;using(var f=zip.GetEntry("xl/workbook.xml").Open())book=XDocument.Load(f);
 bool epoch1904=book.Root.Element(N+"workbookPr")!=null && ((string)book.Root.Element(N+"workbookPr").Attribute("date1904")=="1" || (string)book.Root.Element(N+"workbookPr").Attribute("date1904")=="true");
 var sheet=book.Descendants(N+"sheet").First();string rel=(string)sheet.Attribute(XName.Get("id","http://schemas.openxmlformats.org/officeDocument/2006/relationships"));
 XDocument rels;using(var f=zip.GetEntry("xl/_rels/workbook.xml.rels").Open())rels=XDocument.Load(f);
 var target=(string)rels.Root.Elements().First(x=>(string)x.Attribute("Id")==rel).Attribute("Target");string sheetPath=target.StartsWith("/")?target.TrimStart('/'):("xl/"+target);XDocument data;using(var f=zip.GetEntry(sheetPath).Open())data=XDocument.Load(f);
 var result=new List<SaleLine>();bool header=false;
 foreach(var row in data.Descendants(N+"row")){
 var cells=new Dictionary<string,string>();int ordinal=0;foreach(var c in row.Elements(N+"c")){string reference=(string)c.Attribute("r")??"";string col=new String(reference.TakeWhile(Char.IsLetter).ToArray());if(col.Length==0){int pos=++ordinal;while(pos>0){pos--;col=(char)(65+pos%26)+col;pos/=26;}}else{ordinal=0;foreach(char letter in col)ordinal=ordinal*26+letter-64;}cells[col]=Cell(c,strings);}
 Func<string,string> get=k=>cells.ContainsKey(k)?cells[k].Trim():"";
 if(!header){if(get("D")=="Fecha emisión"&&get("K").Contains("comprobante")&&get("CA").Contains("Venta a precio final")&&get("CG").StartsWith("Utilidad"))header=true;continue;}
 if(get("D")==""&&get("K")=="")continue; // Totales y separadores no son comprobantes.
 DateTime date;double serial;string ds=get("D");if(Double.TryParse(ds,NumberStyles.Float,CultureInfo.InvariantCulture,out serial))date=(epoch1904?new DateTime(1904,1,1).AddDays(serial):DateTime.FromOADate(serial)).Date;else if(!DateTime.TryParse(ds,CultureInfo.InvariantCulture,DateTimeStyles.None,out date))throw new Exception("Fecha inválida en fila "+(string)row.Attribute("r"));
 if(get("K")=="")throw new Exception("Falta comprobante en fila "+(string)row.Attribute("r"));
 if(get("Q")!="Taller"&&get("Q")!="Mostrador")throw new Exception("Sector desconocido: "+get("Q"));
 decimal sale,profit;if(!Decimal.TryParse(get("CA"),NumberStyles.Float,CultureInfo.InvariantCulture,out sale)||!Decimal.TryParse(get("CG"),NumberStyles.Float,CultureInfo.InvariantCulture,out profit))throw new Exception("Importe inválido en fila "+(string)row.Attribute("r"));
 result.Add(new SaleLine{Date=date.Date,Dealer=get("A"),Branch=get("B"),Type=get("H"),Code=get("I"),Letter=get("J"),Number=get("K"),Sector=get("Q"),Part=get("BM"),Sale=sale,Profit=profit});
 }
 if(!header||result.Count==0)throw new Exception("No se encontró un reporte válido con las columnas D, K, CA y CG esperadas.");return result;
 }}
}
public class SalesStore {
 public readonly string Root;public Dictionary<string,Snapshot> History=new Dictionary<string,Snapshot>();public Dictionary<string,Goal> Goals=new Dictionary<string,Goal>();public bool Intern=true,Warranty=true,Courtesy=true;
 public HashSet<DateTime> Holidays=new HashSet<DateTime>();public HashSet<int> CalendarYears=new HashSet<int>();
 static string Key(DateTime p){return p.ToString("yyyy-MM");} static string S(DateTime d){return d.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);}static string V(decimal v){return v.ToString(CultureInfo.InvariantCulture);}static DateTime DT(string s){return DateTime.ParseExact(s,"yyyy-MM-dd",CultureInfo.InvariantCulture);}static decimal DN(string s){return Decimal.Parse(s,CultureInfo.InvariantCulture);}
 public SalesStore(string root){Root=root;Directory.CreateDirectory(root);Load();}
 public Goal GetGoal(DateTime p){string k=Key(p);if(!Goals.ContainsKey(k))Goals[k]=new Goal();return Goals[k];}
 public Snapshot Get(DateTime p){Snapshot value;return History.TryGetValue(Key(p),out value)?value:null;}
 public void Set(Snapshot s){History[Key(s.Period)]=s;}
 public List<SaleLine> Visible(Snapshot s){return s==null?new List<SaleLine>():s.Lines.Where(x=>SalesMath.Included(x,Intern,Warranty,Courtesy)).ToList();}
 public bool HasCalendar(DateTime start,DateTime end){for(int y=start.Year;y<=end.Year;y++)if(!CalendarYears.Contains(y))return false;return true;}
 public void Save(){var root=new XElement("Objetivos",new XAttribute("version",1),new XAttribute("intern",Intern),new XAttribute("warranty",Warranty),new XAttribute("courtesy",Courtesy));
 root.Add(new XElement("Calendars",CalendarYears.OrderBy(x=>x).Select(y=>new XElement("Year",new XAttribute("value",y),Holidays.Where(d=>d.Year==y).OrderBy(d=>d).Select(d=>new XElement("Day",S(d)))))));
 root.Add(new XElement("Goals",Goals.Select(g=>new XElement("Goal",new XAttribute("period",g.Key),new XAttribute("total",V(g.Value.Total)),new XAttribute("taller",V(g.Value.Taller)),new XAttribute("margin",V(g.Value.Margin))))));
 foreach(var s in History.Values){var e=new XElement("Snapshot",new XAttribute("period",Key(s.Period)),new XAttribute("cutoff",S(s.Cutoff)),new XAttribute("loaded",s.Loaded.ToString("o")),new XAttribute("source",s.Source));foreach(var l in s.Lines)e.Add(new XElement("Line",new XAttribute("date",S(l.Date)),new XAttribute("dealer",l.Dealer),new XAttribute("branch",l.Branch),new XAttribute("type",l.Type),new XAttribute("code",l.Code),new XAttribute("letter",l.Letter),new XAttribute("number",l.Number),new XAttribute("sector",l.Sector),new XAttribute("part",l.Part),new XAttribute("sale",V(l.Sale)),new XAttribute("profit",V(l.Profit))));root.Add(e);}
 string path=Path.Combine(Root,"objetivos.xml"),tmp=path+".tmp";new XDocument(root).Save(tmp);if(File.Exists(path))File.Replace(tmp,path,path+".bak");else File.Move(tmp,path);
 }
 void Load(){string path=Path.Combine(Root,"objetivos.xml");if(!File.Exists(path))return;var root=XDocument.Load(path).Root;Intern=(bool)root.Attribute("intern");Warranty=(bool)root.Attribute("warranty");Courtesy=(bool)root.Attribute("courtesy");foreach(var y in root.Element("Calendars").Elements("Year")){CalendarYears.Add((int)y.Attribute("value"));foreach(var d in y.Elements("Day"))Holidays.Add(DT(d.Value));}foreach(var g in root.Element("Goals").Elements("Goal"))Goals[(string)g.Attribute("period")]=new Goal{Total=DN((string)g.Attribute("total")),Taller=DN((string)g.Attribute("taller")),Margin=DN((string)g.Attribute("margin"))};foreach(var s in root.Elements("Snapshot")){var snap=new Snapshot{Period=DT((string)s.Attribute("period")+"-01"),Cutoff=DT((string)s.Attribute("cutoff")),Loaded=DateTime.Parse((string)s.Attribute("loaded"),null,DateTimeStyles.RoundtripKind),Source=(string)s.Attribute("source")};foreach(var l in s.Elements("Line"))snap.Lines.Add(new SaleLine{Date=DT((string)l.Attribute("date")),Dealer=(string)l.Attribute("dealer"),Branch=(string)l.Attribute("branch"),Type=(string)l.Attribute("type"),Code=(string)l.Attribute("code"),Letter=(string)l.Attribute("letter"),Number=(string)l.Attribute("number"),Sector=(string)l.Attribute("sector"),Part=(string)l.Attribute("part"),Sale=DN((string)l.Attribute("sale")),Profit=DN((string)l.Attribute("profit"))});Set(snap);}}
}
}
