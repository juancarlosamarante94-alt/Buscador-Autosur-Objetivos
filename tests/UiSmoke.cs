using System;using System.Drawing;using System.Linq;using System.Windows.Forms;using System.Reflection;using System.Collections.Generic;using AutosurBuscador;
class UiSmoke {
 static IEnumerable<Control> Walk(Control c){foreach(Control child in c.Controls){yield return child;foreach(var nested in Walk(child))yield return nested;}}
 static object Get(object o,string n){return o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);}static void Set(object o,string n,object v){o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,v);}
 static void Refresh(ObjetivosPage page){page.GetType().GetMethod("ResetDates",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(page,null);Application.DoEvents();}
 static void CaptureCanvas(Form f,string name){var root=f.Controls[0];using(var host=new Panel{Size=new Size(1440,1080),BackColor=DashboardTheme.Bg}){f.Controls.Remove(root);host.Controls.Add(root);host.CreateControl();host.PerformLayout();Application.DoEvents();using(var bmp=new Bitmap(host.Width,host.Height)){host.DrawToBitmap(bmp,new Rectangle(0,0,bmp.Width,bmp.Height));bmp.Save(name);}host.Controls.Remove(root);f.Controls.Add(root);f.PerformLayout();}}
 static void Capture(Form f,string name){Application.DoEvents();using(var bmp=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(bmp,new Rectangle(0,0,bmp.Width,bmp.Height));bmp.Save(name);}}
 [STAThread]static void Main(){
 Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
 var store=new SalesStore(System.IO.Path.Combine(System.IO.Path.GetTempPath(),"AutosurUi_"+Guid.NewGuid().ToString("N")));var p=new DateTime(2026,10,1);var days=new List<DateTime>();
 for(var d=SalesMath.Start(p);d<=new DateTime(2026,10,14);d=d.AddDays(1))if(d.DayOfWeek!=DayOfWeek.Saturday&&d.DayOfWeek!=DayOfWeek.Sunday&&d!=new DateTime(2026,10,12))days.Add(d);
 var lines=new List<SaleLine>();decimal tw=days.Select((d,i)=>(decimal)(i%4+2)).Sum(),tLeft=28000000,mLeft=44000000;int number=1;
 for(int i=0;i<days.Count;i++)for(int j=0;j<10;j++){bool last=i==days.Count-1&&j==9;decimal t=last?tLeft:28000000*(i%4+2)/tw/10,m=last?mLeft:44000000*(i%4+2)/tw/10;tLeft-=t;mLeft-=m;string type=j==0?"VENTA INTERNA":j==1?"GARANTIA":"FACTURA DE VENTA";
 lines.Add(new SaleLine{Date=days[i],Type=type,Code="F",Number=(number++).ToString(),Sector="Taller",Sale=t,Profit=t*.30m});
 string key=(number++).ToString();lines.Add(new SaleLine{Date=days[i],Type="FACTURA DE VENTA",Code="F",Number=key,Sector="Mostrador",Sale=m*.6m,Profit=m*.12m});lines.Add(new SaleLine{Date=days[i],Type="FACTURA DE VENTA",Code="F",Number=key,Sector="Mostrador",Sale=m*.4m,Profit=m*.08m});}
 store.Set(new Snapshot{Period=p,Cutoff=new DateTime(2026,10,14),Loaded=DateTime.UtcNow,Source="Reporte de demostración.xlsx",Lines=lines});store.GetGoal(p).Total=60000000;store.GetGoal(p).Margin=24;
 store.Set(new Snapshot{Period=p.AddMonths(-1),Cutoff=SalesMath.End(p.AddMonths(-1)),Loaded=DateTime.UtcNow,Lines=new List<SaleLine>{new SaleLine{Date=new DateTime(2026,9,1),Type="FACTURA DE VENTA",Code="F",Number="prev",Sector="Taller",Sale=50000000,Profit=10000000}}});
 store.CalendarYears.Add(2026);store.Holidays.Add(new DateTime(2026,10,12));
 using(var main=new MainForm()){
 main.MaximumSize=new Size(1920,1400);main.Size=new Size(1440,1080);
 var all=Walk(main).ToArray();var page=all.OfType<ObjetivosPage>().Single();Set(page,"store",store);((DateTimePicker)Get(page,"period")).Value=p;Refresh(page);
 var tabs=all.OfType<TabControl>().First();tabs.SelectedTab=all.OfType<TabPage>().Single(x=>x.Text=="Objetivos");main.Show();Application.DoEvents();
 if(!all.OfType<Button>().Any(x=>x.Text=="Buscar actualizaciones")||!all.OfType<Button>().Any(x=>x.Text=="Acceso GitHub"))throw new Exception("Faltan botones de actualización");
 if(((MetricCard)Get(page,"saleCard")).Value!=72000000m.ToString("C0",System.Globalization.CultureInfo.GetCultureInfo("es-AR")))throw new Exception("Venta visual incorrecta");
 if(!((Label)Get(page,"projection")).Text.Contains("120,00 %"))throw new Exception("Falta sobreperformance total");
 CaptureCanvas(main,"dashboard-verificado.png");
 var sectors=(ComboBox)Get(page,"sector");foreach(int index in new[]{1,2}){sectors.SelectedIndex=index;Application.DoEvents();var card=(MetricCard)Get(page,"advanceCard");decimal value=Decimal.Parse(card.Value.Replace("%","").Trim(),System.Globalization.CultureInfo.GetCultureInfo("es-AR"));if(value<=100)throw new Exception("Falta sobreperformance de "+sectors.Text);}
 sectors.SelectedIndex=0;
 var tickets=(DataGridView)Get(page,"results");if(Convert.ToInt32(tickets.Rows[1].Cells[1].Value)!=days.Count*10)throw new Exception("Se duplicaron tickets por sus líneas");
 main.Size=main.MinimumSize;Application.DoEvents();Capture(main,"dashboard-compacto.png");
 if(((MetricCard)Get(page,"saleCard")).Width<200)throw new Exception("Tarjetas demasiado pequeñas");
 ((DateTimePicker)Get(page,"period")).Value=p.AddMonths(1);Refresh(page);if(!((Label)Get(page,"status")).Text.Contains("Sin datos"))throw new Exception("Falla estado vacío");
 Console.WriteLine("OK interfaz completa, venta, 120% total, sobreperformance de ambas bocas, tickets únicos, tamaño mínimo y estado vacío.");main.Close();}
 }
}