#region using statementen

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.IO;
using Microsoft.CSharp;
using Microsoft.CSharp.RuntimeBinder;
using System.Reflection;
using System.Collections;
using System.Data.Linq;
using System.Data.Entity;
using System.ComponentModel.DataAnnotations;

using System.CodeDom;
using System.CodeDom.Compiler;
using System.Resources;

using System.Diagnostics;
using EnvDTE80;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using EnvDTE;
using System.Data.SqlClient;
using Microsoft.VisualStudio.Shell.Interop;
using System.Runtime.Remoting;
using System.Reflection.Emit;

using System.Data.Common;
using System.Data.Objects.DataClasses;
using System.Data.Metadata.Edm;
using System.Data.Objects;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

using System.Data.Entity.Design.PluralizationServices;
using System.Globalization;

using System.Runtime.Serialization;
using Hyper.ComponentModel;

#endregion

namespace genericdbgenerator
{
    [TypeDescriptionProvider(typeof(HyperTypeDescriptionProvider))]
    public partial class GenerateDB : Form
    {
        public GenerateDB()
        {
            InitializeComponent();
        }
        
        /// <summary>
        /// Generate DB for object
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>    
        private void btnStart_click(object sender, EventArgs e)
        {
            lblNotice.Text = "Working...";

            btnStart.Enabled = false;

            this.Refresh();                                          
          
            //deserialize de XML in een nieuwe instantie van QKB, het hoofdtype van de data. Dit type staat in de class die gegenereerd is op basis van de Quaestor XSD mbv XSD2CODE                              

            qpf qsolution = qpf.LoadFromFile(@"18120.xml");
            qsolution.solutions.RemoveRange(0, 4);
            //qsolution.solutions.RemoveAt(2);
            //qsolution.solutions.RemoveAt(0);
            //genereer db voor qsolution
            
            object[] returners = GenerateTablesForType(qsolution.GetType(), qsolution, true);
   
            //save data in de database
            
            EvaluateStructure(qsolution, returners, 1);
            
            Assembly dassemnbly = (Assembly)returners[0];
            String connectionstring = (String)returners[1];
            Type[] GenericTypes = (Type[])returners[4];
            Type TypeOfMainDbSet = GenericTypes.Where(u => u.FullName == qsolution.GetType().FullName).FirstOrDefault();

            DbContext DbC = (DbContext)returners[5];
             
            PropertyInfo[] DbSets = (PropertyInfo[])returners[6];           
            
            //ObjectDumper.Write(qsolution, 6);
            lblNotice.Text = "Done. Now What?";
            btnStart.Enabled = true;

            this.Refresh();                                                
        }

        /// <summary>
        /// Retrieve data from Database
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_retrieve_Click(object sender, EventArgs e)
        {
            lblNotice.Text = "Working...";

            btnStart.Enabled = false;
            btn_retrieve.Enabled = false;

            this.Refresh();
            
            //uitzoeken welke context gebruikt kan worden
            //generated_PQZMBSHVBN_qpfContext DummyDbc = new generated_PQZMBSHVBN_qpfContext();
            
            Stopwatch stp = new Stopwatch();

            stp.Start();
            //qpf QueriedObject = (qpf)NewGetDataFromGeneratedDatabase(DummyDbc);
            stp.Stop();

            Console.WriteLine("\r\n");
            Console.WriteLine("Done retrieving the data. This is how ludicrously long it took: " + stp.Elapsed);
            Console.WriteLine("\r\n");

            //QueriedObject.SaveToFile("18120_regen.xml");

            lblNotice.Text = "Done. Now What.";            

            btn_retrieve.Enabled = true;
            btnStart.Enabled = true;

            this.Refresh();                                     
        }

        #region 2de query functie
        private object NewGetDataFromGeneratedDatabase(object DatabaseContext)
        {
            //eens kijken of lazy en eager is uitmaakt hier

            ((DbContext)DatabaseContext).Configuration.LazyLoadingEnabled = false;
                                    
            //dus nu heb ik eager loading. nu eens alles laden            
            String type = "DbSet`1";
            PropertyInfo[] DbSets = ((object)DatabaseContext).GetType().GetProperties().Where(a => a.PropertyType.Name == type).ToArray();

            IList AllTabels = new List<object>();
            foreach (PropertyInfo pinf in DbSets)
            {
                IQueryable val = (IQueryable)pinf.GetValue(DatabaseContext, null);
                val.Load();
                AllTabels.Add(val);
            }

            return DatabaseContext;
        }
        #endregion

        #region Eerste query functie, nog niet erg snel
        //dit werkt nog niet        
        private object GetDataFromGeneratedDatabase(object DatabaseContext)
        {
            //versie zonder dbsets, eerst die maar eens ophalen en dan de regular flavor van de functie starten            

            String type = "DbSet`1";
            PropertyInfo[] DbSets = ((object)DatabaseContext).GetType().GetProperties().Where(a => a.PropertyType.Name == type).ToArray();

            IList AllTabels = new List<object>();
            foreach (PropertyInfo pinf in DbSets)
            {
                AllTabels.Add(pinf.GetValue(DatabaseContext, null));
            }

            //weten we waar de database op gegenereerd is? Nee. we weten niet wat het hoogste niveau is... dus daar moet eerst op gequeryd worden. ElementId = 1 dus
            //GetDataFromGeneratedDatabase(DatabaseContext, DbSets);

            //dit word even mijn test functie, om te kijken of het mogelijk is om de hele godganse structuur terug te halen uit de db

            object TopRow = null;
            #region eerst moet ik de row vinden met elementid = 1 (in dit geval dus de QPF) hoe vind ik die?                              

            for (int y = 0; y < AllTabels.Count; y++)
            {
                IQueryable dbset = AllTabels[y] as IQueryable;
                                
                foreach (var row in dbset) 
                {
                    Type rowtype = row.GetType();
                    PropertyInfo[] rowtypeproperties = rowtype.GetProperties();
                    Boolean found = false;

                    for (int u = 0; u < rowtypeproperties.Length; u++)
                    {
                        PropertyInfo p = rowtypeproperties[u];

                        if (p.Name == "ElementId")
                        {
                            int i = (int)p.GetValue(row, null);

                            if (i == 1)
                            {                                
                                found = true;
                                TopRow = row;
                                break;
                            }
                        }
                    }

                    if (found)
                        break;
                }
            }
            #endregion                  

            //nu kan ik TopRow (Qpf dus) gaan uitpluizen. Daarvoor heb ik het type nodig van de waarde. 
            String na = TopRow.GetType().Name.Replace("TFDB","");            
            
            //var TopRowActualType = Activator.CreateInstance(null, "genericdbgenerator." + na).Unwrap();
            
            Type t = Type.GetType("genericdbgenerator." + na);
            ConstructorInfo info = t.GetConstructor(Type.EmptyTypes);
            ObjectCreateMethod inv = new ObjectCreateMethod(info);
            var TopRowActualType = inv.CreateInstance();          
    
            object retu = UnwrapDatabase(TopRow, TopRowActualType, AllTabels);      
            
            return retu;
        }

        private object UnwrapDatabase(object TopRow, object ResultObject, IList AllTables)
        {
            Type ActualType = ResultObject.GetType();
            
            //FieldInfo[] ActualTypeFields = ActualType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);           // 
            PropertyInfo[] ActualTypeFields = ActualType.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);            

            PropertyInfo[] TopRowProperties = TopRow.GetType().GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

            int TopRowPropertiesLength = TopRowProperties.Length;
            
            int AllTablesCount = AllTables.Count;

            for (int i = 0; i < TopRowPropertiesLength; i++)
            {
                PropertyInfo TRPinfo = TopRowProperties[i];
                String TopType = TRPinfo.Name;
          
                //nu moet ik met de naam van het veld het overeenkomstige veld in het overeenkomstige type vinden.

                PropertyInfo[] ap = ActualTypeFields.Where(u => u.Name == TopType).ToArray();

                if (ap.Length != 0)
                {
                    PropertyInfo ParallelField = ap[0];
                    //nu hebben we het overeenkosmtige veld, aan het type daarvan kunnen we zien of de waarde een verwijzing is. 
                    
                    Type ParallelFieldType = ParallelField.PropertyType;
                    //var value = TRPinfo.GetValue(TopRow, null);

                    PropertyDescriptorCollection props = TypeDescriptor.GetProperties(TopRow);
                    var value = props[TRPinfo.Name].GetValue(TopRow);
                    
                    //dit doen we met die lelijke hack
                    object[] atts = Assembly.GetAssembly(ParallelFieldType).GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                   
                    if (ParallelFieldType.IsGenericType)
                    {
                        //eerst alle rijen ophalen die achter elementId zitten, die in een lijst proppen. 
                        //int ElementId = (int)TRPinfo.GetValue(TopRow, null);

                        PropertyDescriptorCollection subprops = TypeDescriptor.GetProperties(TopRow);
                        int ElementId = (int)subprops[TRPinfo.Name].GetValue(TopRow);
                                                
                        String TargetTableName = ParallelFieldType.GetGenericArguments()[0].Name + "TFDB";                       
                        
                        List<object> rowlist = new List<object>();
                                           
                        Type a = ParallelFieldType.GetGenericArguments()[0];
                        object[] atts2 = Assembly.GetAssembly(a).GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                        
                        IList reftypelist = null;

                        if (atts2.Length != 0 && String.Equals(((AssemblyProductAttribute)atts2[0]).Product, "Microsoft® .NET Framework"))
                        {
                            //maak een normale lijst
                            Type customlist = typeof(List<>).MakeGenericType(a);
                            reftypelist = (IList)Activator.CreateInstance(customlist);
                            TargetTableName = "MicrosoftTypeList";
                        }
                        else
                        {
                            Type ListType = Type.GetType("genericdbgenerator." + a.Name);                          
                            //Type customlist = typeof(List<>).MakeGenericType(Activator.CreateInstance(null, "genericdbgenerator." + a.Name).Unwrap().GetType());
                            reftypelist = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(ListType));                                                     
                        }                       

                        for (int o = 0; o < AllTablesCount; o++)
                        {
                            dynamic dbset = AllTables[o];
                            

                            if (((IQueryable)dbset).ElementType.Name == TargetTableName)
                            {
                                int dbsetcount = dbset.Local.Count;

                                for(int l = 0;l < dbsetcount;l++)                                
                                {
                                    var row = dbset.Local[l];
                                    Type rowtype = row.GetType();
                                    //PropertyInfo rowtypeproperties = rowtype.GetProperties().Where(u => u.Name == "ElementId").ToArray()[0];
                                    //int innerElementId = (int)rowtypeproperties.GetValue(row, null);

                                    PropertyDescriptorCollection innerprops = TypeDescriptor.GetProperties(rowtype);
                                    int innerElementId = (int)innerprops["ElementId"].GetValue(row);
                                
                                    if (innerElementId == ElementId)
                                    {
                                        rowlist.Add(row);
                                    }
                                }
                            }
                        }

                        int rowlistCount = rowlist.Count;

                        for (int u = 0; u < rowlistCount; u++)
                        {
                            var row = rowlist[u];
                            //hier moet nog gechecked worden of het gaat om een speciaal type of een microsoft lijst...

                            if (atts2.Length != 0 && String.Equals(((AssemblyProductAttribute)atts2[0]).Product, "Microsoft® .NET Framework"))
                            {
                                Console.WriteLine("");
                            }
                            else
                            {
                                //var RefType = Activator.CreateInstance(null, "genericdbgenerator."+a.Name).Unwrap();

                                Type t = Type.GetType("genericdbgenerator." + a.Name);
                                ConstructorInfo info = t.GetConstructor(Type.EmptyTypes);
                                ObjectCreateMethod inv = new ObjectCreateMethod(info);

                                var RefType = inv.CreateInstance();

                                RefType = UnwrapDatabase(row, RefType, AllTables);
                                reftypelist.Add(RefType);
                            }
                        }

                        PropertyDescriptorCollection setprops = TypeDescriptor.GetProperties(ResultObject);                                                                       
                        setprops[ParallelField.Name].SetValue(ResultObject, reftypelist);
                        //setprops.
                        //ParallelField.SetValue(ResultObject, reftypelist, null);                        
                    }
                    else if (atts.Length != 0 && String.Equals(((AssemblyProductAttribute)atts[0]).Product, "Microsoft® .NET Framework"))
                    {
                        //normaal type, behandel de waarde niet als verwijzing maar gewoon als... waarde! maar wacht. Als er iets in de waarde staat moeten we nog iets anders doen.
                        String[] o = new string[] { "<--->" };
                        String[] splutter = null;

                        if(value!=null)
                            splutter = value.ToString().Split(o, StringSplitOptions.RemoveEmptyEntries);

                        if (splutter != null)
                        {
                            if (splutter.Length == 1)
                            {
                                PropertyDescriptorCollection setprops = TypeDescriptor.GetProperties(ResultObject);
                                setprops[ParallelField.Name].SetValue(ResultObject, value);

                                //ParallelField.SetValue(ResultObject, value, null);
                            }
                            else
                            {
                                //speciaal type, behandel waarde als verwijzingsnummer.
                                int ElementId = Int32.Parse(splutter[0]);
                                
                                //het resultaat moet nu een instantie krijgen van het type wat hier gevonden is. 
                                //var RefType = Activator.CreateInstance(null, "genericdbgenerator." + ParallelFieldType.Name).Unwrap();

                                object RefType = null;
                                //nu moet ik de rij vinden achter ElementId
                                object NewTopRow = null;

                                String na = TopRow.GetType().Name.Replace("TFDB", "");                                

                                for (int y = 0; y < AllTablesCount; y++)
                                {
                                    //IQueryable dbset = AllTables[y] as IQueryable;
                                    dynamic dbset = AllTables[y];
                                    Boolean found = false;

                                    int dbsetcount = dbset.Local.Count;

                                    for (int l = 0; l < dbsetcount; l++)
                                    //foreach (var row in dbset)
                                    {
                                        var row = dbset.Local[l];
                                        Type rowtype = row.GetType();

                                        //PropertyInfo rowtypeproperties = rowtype.GetProperties().Where(u => u.Name == "ElementId").ToArray()[0];
                                        //int innerElementId = (int)rowtypeproperties.GetValue(row, null);

                                        PropertyDescriptorCollection innerprops = TypeDescriptor.GetProperties(rowtype);
                                        int innerElementId = innerprops["ElementId"].GetValue(row);

                                        if (innerElementId == ElementId)
                                        {
                                            String actual = rowtype.Name.Replace("TFDB", "");
                                            //RefType = Activator.CreateInstance(null, "genericdbgenerator." + actual).Unwrap();

                                            Type t = Type.GetType("genericdbgenerator." + actual);
                                            ConstructorInfo info = t.GetConstructor(Type.EmptyTypes);
                                            ObjectCreateMethod inv = new ObjectCreateMethod(info);
                                            RefType = inv.CreateInstance();

                                            NewTopRow = row;
                                            found = true;
                                            break;
                                        }
                                    }
                                    if (found)
                                        break;
                                }

                                //voordat ik m set als waarde in het veld moet ie eerst gevuld worden. 
                                RefType = UnwrapDatabase(NewTopRow, RefType, AllTables);

                                PropertyDescriptorCollection setprops = TypeDescriptor.GetProperties(ResultObject);
                                setprops[ParallelField.Name].SetValue(ResultObject, RefType);

                                //ParallelField.SetValue(ResultObject, RefType, null);
                            }
                        }                          
                        else
                        {
                            PropertyDescriptorCollection setprops = TypeDescriptor.GetProperties(ResultObject);
                            setprops[ParallelField.Name].SetValue(ResultObject, value);

                            //ParallelField.SetValue(ResultObject, value, null);
                        }
                    }
                    else
                    {
                        //speciaal type, behandel waarde als verwijzingsnummer.               
                        
                        PropertyDescriptorCollection subprops = TypeDescriptor.GetProperties(TopRow);
                        int ElementId = (int)subprops[TRPinfo.Name].GetValue(TopRow);

                        //het resultaat moet nu een instantie krijgen van het type wat hier gevonden is. 
                        //var RefType = Activator.CreateInstance(null, "genericdbgenerator." + ParallelFieldType.Name).Unwrap();

                        Type t = Type.GetType("genericdbgenerator." + ParallelFieldType.Name);
                        ConstructorInfo info = t.GetConstructor(Type.EmptyTypes);
                        ObjectCreateMethod inv = new ObjectCreateMethod(info);
                        var RefType = inv.CreateInstance();

                        //nu moet ik de rij vinden achter ElementId
                        object NewTopRow = null;
                        for (int y = 0; y < AllTablesCount; y++)
                        { 
                            dynamic dbset = AllTables[y];

                            Boolean found = false;

                            String a = RefType.GetType().Name + "TFDB";

                            if (((IQueryable)dbset).ElementType.Name == a)
                            {                  
                                int dbsetcount = dbset.Local.Count;

                                for(int l = 0; l < dbsetcount; l++)                                
                                {
                                    var row = dbset.Local[l];

                                    Type rowtype = row.GetType();                                 
                                    
                                    PropertyDescriptorCollection innerprops = TypeDescriptor.GetProperties(rowtype);
                                    int innerElementId = innerprops["ElementId"].GetValue(row);

                                    if (innerElementId == ElementId)
                                    {
                                        NewTopRow = row;
                                        found = true;
                                        break;
                                    }
                                }
                            }
                            if (found)
                                break;
                        }
                        
                        //voordat ik m set als waarde in het veld moet ie eerst gevuld worden. 
                        RefType = UnwrapDatabase(NewTopRow, RefType, AllTables);

                        PropertyDescriptorCollection setprops = TypeDescriptor.GetProperties(ResultObject);
                        setprops[ParallelField.Name].SetValue(ResultObject, RefType);                        

                        //ParallelField.SetValue(ResultObject, RefType, null);
                    }
                }             
            }

            return ResultObject;
        }

        private object GetDataFromGeneratedDatabase(DbContext DatabaseContext, PropertyInfo[] DbSets)
        {
            solutionType search = new solutionType(); //dit word een parameter die user moet geven

            String parametername = "codbField";
            String parametervalue = "213";
            
            //ik ken de type van search, dus ik kan de tabel targeten. denkik.           
            
            //eerst moet ik de DbSet vinden met het type parameterType
            Type TypeOfSearchParam = search.GetType();
            PropertyInfo[] TypeOfSearchParamProperties = TypeOfSearchParam.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            IEnumerable DbSet = null;

            foreach (PropertyInfo pinfo in DbSets)
            {
                String n = pinfo.PropertyType.GetGenericArguments()[0].Name;
                if (n == TypeOfSearchParam.Name)
                {
                    DbSet = (IEnumerable)pinfo.GetValue(DatabaseContext, null);
                    break;
                }
            }                    

            var DbSetEnum = DbSet.GetEnumerator();
            //nu heb ik de tabel van hetgene waarop gequeryd word in de vorm van DbSet

            int i = 0;

            //dit word de lijst met de resultaten? gokje
            List<object> ResultsList = new List<object>();

            while (DbSetEnum.MoveNext())
            {
                var a = DbSetEnum.Current;

                PropertyInfo SearchProperty = a.GetType().GetProperty(parametername, BindingFlags.Public | BindingFlags.Instance);
                var z = SearchProperty.GetValue(a, null);

                if (z.ToString() == parametervalue)
                { 
                    //hebbes. we hebben er 1 die voldoet aan de opgegeven search delimiter, HUP DE LIJST IN
                    ResultsList.Add(a);
                }               

                i++;
            }

            /* nu hebben we in Results de resultaten. nu kunnen we een lijstje gaan 
             * bijhouden met als type het type wat de user op heeft gegeven. deze lijst 
             * gaan we dan per element vullen met w/e de verwijzingen heen gaan
             */            
            
            for (int o = 0; o < ResultsList.Count; o++ )
            {
                List<PropertyInfo> prop = ResultsList[o].GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).ToList().Where(u => u.Name != "ElementId" && u.Name != "Id").ToList();
                
                var RetrievedTableObject = ResultsList[o];

                for (int u = 0; u < prop.Count; u++)
                {
                    //kijken wat voor een type het moet zijn. 
                    Type ShouldBeOfType = TypeOfSearchParamProperties[u].PropertyType;
                    var FieldValue = prop[u].GetValue(RetrievedTableObject, null);
                    Type ActualType = FieldValue.GetType();

                    if (ActualType == ShouldBeOfType)
                    {
                        //prima, zelfde type. dit is geen verwijzing. 
                        float a = 0;
                        a++;
                    }
                    else
                    {
                        //dit is een verwijzing
                        float y = 0;
                        y++;
                    }
                }
            }

            return null;
        }

        #endregion

        #region Opslaan van gegevens
        public void EvaluateStructure(object Structure, object[] returners, Int32 ElementId, Boolean IncludeGeneratedContextInProject = false)
        {
            /*
             * Nu hebben we een database en, belangrijker, een database context. Hiermee kunnen we gaan opslaan. Bam!
             */
            
            /*
             * 7 juli 2012: 
             * 
             * De verwijzingen kloppen niet. Bij lijsten telt ie gewoon vrolijk door als er weer een speciaal type word gevonden.. dit moet niet. 
             */

            #region Bulk vs SaveChanges()
            /*
            MethodInfo method1 = typeof(DbSet<Movie>).GetMethod("Add", new[] { typeof(Movie) });
            LateBoundMethod callback = DelegateFactory.Create(method1);

            Stopwatch stp = new Stopwatch();
            stp.Start();

            SqlConnection con = new SqlConnection("Data Source=localhost;Initial Catalog=xmltest.TestContext;Integrated Security=True;Pooling=False;");

            TestContext td = new TestContext();
            td.Database.CreateIfNotExists();
            td.Configuration.AutoDetectChangesEnabled = false;

            int iterations = 100000;

            // create a dynamic delegate from the method                       
            List<Movie> Movies = new List<Movie>();
            
            for (int i = 0; i < iterations; i++)
            {
                // call the method
                Movie testmovie = new Movie();
                testmovie.Year = 2012;
                testmovie.Name = "titan ae";
                
                Movies.Add(testmovie);

                callback(td.Movies, new[] { testmovie });
            }            

            var bulkbulk = new SqlBulkCopy("Data Source=localhost;Initial Catalog=xmltest.TestContext;Integrated Security=True;Pooling=False;");
            bulkbulk.BulkCopyTimeout = 120;
            bulkbulk.DestinationTableName = "dbo.Movies";
            bulkbulk.WriteToServer(Movies.AsDataReader());

            //td.SaveChanges();

            stp.Stop();
            Console.WriteLine("ELAPSED TIME: " + stp.Elapsed);
            */
            #endregion          
            
            Type fullstructure = Structure.GetType();            
            Stopwatch stp = new Stopwatch();
            stp.Start();
                     
            Assembly dassemnbly = (Assembly)returners[0];
            String connectionstring = (String)returners[1];
            
            //ListOfTableLists is de lijst waarin alle data komt. Per tabel is een lijst in deze lijst. 
            List<IList> ListOfTableLists = (List<IList>)returners[2];
            String[] TableNames = (String[])returners[3];
            Type[] GenericTypes = (Type[])returners[4];
            
            //System.Reflection.FieldInfo[] fiarrayfull = fullstructure.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

            PropertyInfo[] fiarrayfull = fullstructure.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

            //tabel voor het hoofdtype (qpf), die vullen we later aan de hand van de valuetype                
            String TypeOfMainDbSetName = Structure.GetType().FullName;
            Type TypeOfMainDbSet = GenericTypes.Where(u => u.FullName == TypeOfMainDbSetName + "TFDB").FirstOrDefault();
            var NewDbSetType = Activator.CreateInstance(dassemnbly.FullName, TypeOfMainDbSet.FullName).Unwrap();
            PropertyInfo[] NewDbProperties = NewDbSetType.GetType().GetProperties();

            //zet het element id. in dit geval is het 1
            NewDbProperties.Where(u => u.Name == "ElementId").FirstOrDefault().SetValue(NewDbSetType, 1, null);                       

            foreach (PropertyInfo field in fiarrayfull)
            {                                             
                var value = field.GetValue(Structure, null);               
              
                /*
                 * Ok, nu begint het echte werk, we hebben een DbContext en een manier om daar data in te klappen. Het hoofdtype is ook een tabel dus daar kunnen we alvast eentje voor  maken.
                 */
            
                if (value == null)
                { 
                    //niks doen, geen value 
                }
                else
                {
                    Type TypeOfValue = value.GetType();
                    PropertyInfo[] ValueProperties = TypeOfValue.GetProperties();
                    Type[] TypeOfValueGenericArguments = TypeOfValue.GetGenericArguments();
                    object[] atts = Assembly.GetAssembly(value.GetType()).GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                    Boolean HasGenericType = TypeOfValueGenericArguments.Length != 0;

                    //check of de value een lijst is
                    if (value is IEnumerable && TypeOfValue != typeof(String))
                    {
                        ElementId++; //deze word nu 3

                        //lijst, de entry voor dit veld in de tabel word een verwijzing.                                              
                        for (int i = 0; i < NewDbProperties.Length; i++)
                        {
                            PropertyInfo p = NewDbProperties[i];
                            if (p.Name == field.Name)
                            {
                                //dit voegt de elementid toe als waarde                                                         
                                //p.SetValue(NewDbSetType, ElementId, null); //bij solutionField wordt dit elementId = 3
                                break;
                                
                            }
                        }
                        
                        Type TypeOfSubDbSet = null;
                        Boolean found = false;
                        if (HasGenericType)
                        {
                            String FieldTypeName = TypeOfValueGenericArguments[0].FullName;

                            for (int i = 0; i < GenericTypes.Length; i++)
                            {
                                Type p = GenericTypes[i];
                                if (p.FullName == FieldTypeName)
                                {
                                    TypeOfSubDbSet = p;
                                    found = true;
                                    break;
                                }
                            }
                        }
                        if (!found)
                        {
                            for (int i = 0; i < GenericTypes.Length; i++)
                            {
                                Type p = GenericTypes[i];
                                if (p.Name == "MicrosoftTypeList")
                                {
                                    TypeOfSubDbSet = p;
                                    break;
                                }
                            }
                        }

                        //de lijst moet nu worden uitgezocht, de ElementId wordt meegegeven, elk element krijgt deze elementid en zo vinden we later de element van de lijst terug.
                        ElementId = EvaluateList(value as IEnumerable, ElementId, TypeOfSubDbSet, dassemnbly, ListOfTableLists, GenericTypes);
                    }
                    else if (atts.Length != 0 && String.Equals(((AssemblyProductAttribute)atts[0]).Product, "Microsoft® .NET Framework") || (TypeOfValue == typeof(object) && ValueProperties.Length == 0))
                    {
                        //standaard type, de tabel heeft dus een veld met een standaard type met de naam van field                        
                        for (int i = 0; i < NewDbProperties.Length; i++)
                        {
                            PropertyInfo p = NewDbProperties[i];
                            if (p.Name == field.Name)
                            {
                                //p.SetValue(NewDbSetType, value, null);
                                break;
                            }
                        }

                        //NewDbSetType.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(u => u.Name == field.Name).FirstOrDefault().SetValue(NewDbSetType, value, null);                       
                    }
                    else
                    {
                        //speciaal type, de entry voor dit veld in de tabel word een verwijzing.                    
                        ElementId++; //deze word nu 2

                        for (int i = 0; i < NewDbProperties.Length; i++)
                        {
                            PropertyInfo p = NewDbProperties[i];
                            if (p.Name == field.Name)
                            {
                                //p.SetValue(NewDbSetType, ElementId, null);
                                break;
                            }
                        }                     

                        //we kunnnen ook al zien in welke tabel de verwijzing gaat uitkomen, want we hebben het fieldtype. daarkunnen we alvast een DbSet van maken, die gaat naar EvaluateSpecialType
                        Type TypeOfSubDbSet = null;
                        String FieldTypeName = field.PropertyType.FullName;
                        for (int i = 0; i < GenericTypes.Length; i++)
                        {
                            Type p = GenericTypes[i];
                            if (p.FullName == FieldTypeName + "TFDB")
                            {
                                TypeOfSubDbSet = p;
                                break;
                            }
                        }

                        var NewSubDbSetType = Activator.CreateInstance(dassemnbly.FullName, TypeOfSubDbSet.FullName).Unwrap();

                        //NewSubDbSetType staat aan het einde van de verwijzing. De ElementId van deze is dus hetzelfde als de int ingevuld in het verwijzings veld van NewDbSetType
                        PropertyInfo[] NewSubDbSetTypeProperties = NewSubDbSetType.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        for (int i = 0; i < NewSubDbSetTypeProperties.Length; i++)
                        {
                            PropertyInfo p = NewSubDbSetTypeProperties[i];
                            if (p.Name == "ElementId")
                            {
                               // p.SetValue(NewSubDbSetType, ElementId, null);
                                break;
                            }
                        }                  
                        
                        //NewSubDbSetType staat aan het einde van de verwijzing. De ElementId van deze is dus hetzelfde als de int ingevuld in het verwijzings veld van NewDbSetType                  
                        EvaluateSpecialType(value, ref ElementId, TypeOfSubDbSet, NewSubDbSetType, dassemnbly, ListOfTableLists, GenericTypes);
                    }

                    //ElementId++;
                }                
            }
            
            //ok, nu kunnen we de boel in de tabel zetten. Eerst propertyinfo van de betreffende DbSet ophalen            
            String ndbst = NewDbSetType.GetType().FullName;                        
            for(int i = 0; i < GenericTypes.Length; i++)
            {
                Type p = GenericTypes[i];
                if (p.FullName == ndbst)
                {
                    ListOfTableLists[i].Add(NewDbSetType);
                    break;
                }
            }

            //CopyToDatabase(ListOfTableLists, TableNames, GenericTypes, connectionstring);
            
            //dispose data
            for (int i = 0; i < ListOfTableLists.Count; i++)
            {
                ListOfTableLists[i].Clear();
            }
       
            stp.Stop();
            Console.WriteLine("SAVING OF " + fullstructure.Name + " OBJECT COMPLETED. ELAPSED TIME: " + stp.Elapsed);
            
            btnStart.Enabled = true;
            lblNotice.Text = "Done";            
        }
        
        public int EvaluateList(IEnumerable ListToEvaluate, Int32 ElementId, Type RecursionType, Assembly DataAssembly, List<IList> ListOfTableLists, Type[] GenericTypes)
        {            
            //SubDbSet neemt nu de rol over van NewDbSet in EvaluateStructure, SubDbSet is de tabel waar deze lijst in gaat

            //ok we hebben dus een lijst, deze moeten we evalueren. Pak de enumerator
            IEnumerator enumerator = ListToEvaluate.GetEnumerator();

            int CountingElementId = ElementId;

            while (enumerator.MoveNext())
            {
                //
                var SubDbSet = Activator.CreateInstance(DataAssembly.FullName, RecursionType.FullName).Unwrap();
                //

                PropertyInfo[] NewDbProperties = SubDbSet.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                var value = enumerator.Current;
                          
                //dit is eigenlijk een lelijke hack, maar soms gaat het deserializen niet goed en zijn alle waarden XmlNodes, die IEnumerable zijn. Als dit zo is duurt het opslaan heel veel langer
                //omdat dan String values per character worden opgeslagen. Deze hack voorkomt dit door value in de value van xmlnode te veranderen.
                Boolean IsXmlNode = value.GetType() == typeof(XmlText);
                if (IsXmlNode) value = ((XmlText)value).Value.Trim();

                Type TypeOfValue = value.GetType();
                object[] atts = Assembly.GetAssembly(TypeOfValue).GetCustomAttributes(typeof(AssemblyProductAttribute), false);

                Type[] TypeOfValueGenericArguments = TypeOfValue.GetGenericArguments();
                Boolean HasGenericType = TypeOfValueGenericArguments.Length != 0;

                if (value is IEnumerable && TypeOfValue != typeof(String))
                {
                    //value is een lijst! Een lijst in een lijst, moet kunnen.
                  
                    for (int i = 0; i < NewDbProperties.Length; i++)
                    {
                        PropertyInfo p = NewDbProperties[i];
                        if (p.Name == "Value")
                        {
                            //value wordt een verwijzing naar een andere lijst. 
                            //p.SetValue(SubDbSet, ElementId.ToString(), null);
                            break;
                        }
                    }                    

                    //we kunnnen ook al zien in welke tabel de verwijzing gaat uitkomen, want we hebben het fieldtype. daarkunnen we alvast een DbSet van maken, die gaat naar EvaluateList
                    Type TypeOfSubDbSet = null;
                    
                    //deze try catch is om te voorkomen dat het veld niet gevonden word omdat het om een type gaat die ik over het hoofd heb gezien, zoals XmlNode. Als dit zo is gaat het naar de microsoft list                                       
                    Boolean found = false;
                    if (HasGenericType)
                    {
                        String FieldTypeName = TypeOfValueGenericArguments[0].FullName;

                        for (int i = 0; i < GenericTypes.Length; i++)
                        {
                            Type p = GenericTypes[i];
                            if (p.FullName == FieldTypeName + "TFDB")
                            {
                                TypeOfSubDbSet = p;
                                found = true;
                                break;
                            }
                        }
                    }                    
                    if (!found)
                    {
                        for (int i = 0; i < GenericTypes.Length; i++)
                        {
                            Type p = GenericTypes[i];
                            if (p.Name == "MicrosoftTypeList")
                            {
                                TypeOfSubDbSet = p;                                
                                break;
                            }
                        }                        
                    }

                    /*
                     * 
                     * 
                     * DOORTELLEN OF NIET??
                     * 
                     * 
                     */

                    ElementId = EvaluateList(value as IEnumerable, CountingElementId, TypeOfSubDbSet, DataAssembly, ListOfTableLists, GenericTypes);
                }
                else if (atts.Length != 0 && String.Equals(((AssemblyProductAttribute)atts[0]).Product, "Microsoft® .NET Framework") || (TypeOfValue == typeof(object) && TypeOfValue.GetProperties().Count() == 0))
                {
                    /* value is een Microsoft type, dus zeer waarschijnlijk een .NET type. Dit is link, maar ik kan nergens iets fatsoenlijks vinden wat echt checked of het een native type is
                    * kan ook nog checken of het type een primitive is, een string, een decimal enz om het echt zeker te weten.
                    * 
                    * Ik check of het geen object is, want object kan velden bevatten. 
                    */

                    for (int i = 0; i < NewDbProperties.Length; i++)
                    {
                        PropertyInfo p = NewDbProperties[i];
                        if (p.Name == "ElementId")
                        {
                            //p.SetValue(SubDbSet, ElementId, null);
                            break;
                        }
                    }
                    
                    //standaard type, de tabel heeft dus een veld met een standaard type met de naam van field
                    Boolean found = false;
                    if (HasGenericType)
                    {
                        String FieldTypeName = TypeOfValueGenericArguments[0].FullName;

                        for (int i = 0; i < NewDbProperties.Length; i++)
                        {
                            PropertyInfo p = NewDbProperties[i];
                            if (p.Name == TypeOfValue.Name + "TFDB")
                            {
                                //p.SetValue(SubDbSet, value, null);
                                break;
                            }
                        }                     
                    }
                    if(!found)
                    {
                        for (int i = 0; i < NewDbProperties.Length; i++)
                        {
                            PropertyInfo p = NewDbProperties[i];
                            if (p.Name == "Value")
                            {
                                //p.SetValue(SubDbSet, value.ToString(), null);
                                break;
                            }
                        }                        
                    }
                    //als de item in deze lijst een standaard type is zal het wel geen speciaal veld hebben in de structuur, dus naam kan null blijven, weten we ook direct dat het onderdeel is van een lijst
                 
                }
                else
                {
                    //speciaal type, bijvoorbeeld solutionType. Hier moet een verwijzing voor gemaakt worden                    
                    //en de entry voor dit veld in de tabel word een verwijzing.                                                             

                    //we kunnnen ook al zien in welke tabel de verwijzing gaat uitkomen, want we hebben het fieldtype. daarkunnen we alvast een DbSet van maken, die gaat naar EvaluateSpecialType
                    Type TypeOfSubDbSet = null;
                    for (int i = 0; i < GenericTypes.Length; i++)
                    {
                        Type p = GenericTypes[i];
                        if (p.FullName == TypeOfValue.FullName + "TFDB")
                        {
                            TypeOfSubDbSet = p;
                            break;
                        }
                    }

                    var NewSubDbSetType = Activator.CreateInstance(DataAssembly.FullName, TypeOfSubDbSet.FullName).Unwrap();

                    PropertyInfo[] NewSubDbSetTypeProperties = NewSubDbSetType.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

                    //NewSubDbSetType staat aan het einde van de verwijzing. De ElementId van deze is dus hetzelfde als de int ingevuld in het verwijzings veld van NewDbSetType
                    for (int i = 0; i < NewSubDbSetTypeProperties.Length; i++)
                    {
                        PropertyInfo p = NewSubDbSetTypeProperties[i];
                        if (p.Name == "ElementId")
                        {                            
                            //p.SetValue(NewSubDbSetType, ElementId, null);
                            break;
                        }
                    }

                    //waarschijnlijk een speciaal type. Op naar EvaluateSpecialType                
                 
                    EvaluateSpecialType(value, ref CountingElementId, TypeOfSubDbSet, NewSubDbSetType, DataAssembly, ListOfTableLists, GenericTypes, true);                      
                }


                /*
                 * ACHTERHAALD: dit hoeft niet, want het opslaan gebeurd al in EvaluateSpecialType. Daarom kreeg je ook stapels lege records...
                 * 
                //ok, nu kunnen we de boel in de tabel zetten. Eerst propertyinfo van de betreffende DbSet ophalen                
                String ndbst = SubDbSet.GetType().FullName;                
                for (int i = 0; i < GenericTypes.Length; i++)
                {
                    Type p = GenericTypes[i];
                    if (p.FullName == ndbst)
                    {
                        //ListOfTableLists[i].Add(SubDbSet);
                        break;
                    }
                }
                */

                //ElementId++;
            }

            return CountingElementId;
        }

        private void EvaluateSpecialType(object SpecialType, ref Int32 ElementId, Type RecursionType, object SubDbSet, Assembly DataAssembly, List<IList> ListOfTableLists, Type[] GenericTypes, Boolean FromList = false)
        {           
            /*
             * SubDbSet is hetgene wat ik toevoeg aan een DbSet
             */                        
            
            //FieldInfo[] FieldsInValue = SpecialType.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            PropertyInfo[] FieldsInValue = SpecialType.GetType().GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

            PropertyInfo[] SubDbSetProperties = SubDbSet.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            //voor elk veld moet er nu het bekende type vergelijkingstruukje worden uitgevoerd
            foreach (PropertyInfo field in FieldsInValue)
            {                
                var value = field.GetValue(SpecialType, null);                

                if (value == null)
                { }
                else
                {
                    //dit is eigenlijk een lelijke hack, maar soms gaat het deserializen niet goed en zijn alle waarden XmlNodes, die IEnumerable zijn. Als dit zo is duurt het opslaan heel veel langer
                    //omdat dan String values per character worden opgeslagen. Deze hack voorkomt dit door value in de value van xmlnode te veranderen.                    
                    if (value.GetType() == typeof(XmlText)) value = ((XmlText)value).Value.Trim();
                    if (value.GetType() == typeof(XmlNode[]) && ((XmlNode[])value).Length == 1) value = ((XmlNode[])value)[0].Value.Trim();


                    Type TypeOfField = field.PropertyType;
                    Type TypeOfValue = value.GetType();

                    object[] atts = Assembly.GetAssembly(TypeOfValue).GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                    Type[] TypeOfValueGenericArguments = TypeOfValue.GetGenericArguments();
                    PropertyInfo[] FieldProperties = TypeOfValue.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                    Boolean HasGenericType = TypeOfValueGenericArguments.Length != 0;

                    //is deze value een list? verder met evaluatelist dan

                    if (value is IEnumerable && TypeOfValue != typeof(String)) //string heeft een enum, negeren die hap
                    {
                        ElementId++; //deze word nu 3

                        //lijst, de entry voor dit veld in de tabel word een verwijzing.                                              
                        for (int i = 0; i < SubDbSetProperties.Length; i++)
                        {
                            PropertyInfo p = SubDbSetProperties[i];
                            if (p.Name == field.Name)
                            {
                                //dit voegt de elementid toe als waarde
                                //p.SetValue(SubDbSet, ElementId, null); //bij solutionField wordt dit elementId = 3
                                break;
                            }
                        }

                        Type TypeOfSubDbSet = null;
                        Boolean found = false;
                        if (HasGenericType)
                        {
                            String FieldTypeName = TypeOfValueGenericArguments[0].FullName;

                            for (int i = 0; i < GenericTypes.Length; i++)
                            {
                                Type p = GenericTypes[i];
                                if (p.FullName == FieldTypeName + "TFDB")
                                {
                                    TypeOfSubDbSet = p;
                                    found = true;
                                    break;
                                }
                            }
                        }
                        if (!found)
                        {
                            for (int i = 0; i < GenericTypes.Length; i++)
                            {
                                Type p = GenericTypes[i];
                                if (p.Name == "MicrosoftTypeList")
                                {
                                    TypeOfSubDbSet = p;
                                    break;
                                }
                            }
                        }

                        //de lijst moet nu worden uitgezocht, de ElementId wordt meegegeven, elk element krijgt deze elementid en zo vinden we later de element van de lijst terug.
                        ElementId = EvaluateList(value as IEnumerable, ElementId, TypeOfSubDbSet, DataAssembly, ListOfTableLists, GenericTypes);
                                   
                    }
                    else if (atts.Length != 0 && String.Equals(((AssemblyProductAttribute)atts[0]).Product, "Microsoft® .NET Framework") || ((TypeOfValue == typeof(object) && FieldProperties.Length == 0)))
                    {                        
                        for (int i = 0; i < SubDbSetProperties.Length; i++)
                        {
                            PropertyInfo p = SubDbSetProperties[i];
                            if (p.Name == field.Name)
                            {
                                if (value.GetType() == typeof(object))
                                {
                                    //p.SetValue(SubDbSet, null, null);
                                    break;
                                }
                                else
                                {
                                    //p.SetValue(SubDbSet, value, null);
                                    break;
                                }
                            }
                        }                                                
                    }
                    else
                    {                                            
                        //speciaal type, de entry voor dit veld in de tabel word een verwijzing.                         
                        //ElementId van subset is al geset
                        Type TypeOfSubDbSet = null;
                        
                        ElementId++;

                        for (int i = 0; i < GenericTypes.Length; i++)
                        {
                            Type p = GenericTypes[i];
                            if (p.FullName == TypeOfValue.FullName + "TFDB")
                            {
                                TypeOfSubDbSet = p;
                                break;
                            }
                        }

                        //we kunnnen ook al zien in welke tabel de verwijzing gaat uitkomen, want we hebben het fieldtype. daarkunnen we alvast een DbSet van maken, die gaat naar EvaluateSpecialType                        
                        var NewSubDbSetType = Activator.CreateInstance(DataAssembly.FullName, TypeOfSubDbSet.FullName).Unwrap();

                        //NewSubDbSetType staat aan het einde van de verwijzing. De ElementId van deze is dus hetzelfde als de int ingevuld in het verwijzings veld van NewDbSetType
                        PropertyInfo[] NewSubDbSetTypeProperties = NewSubDbSetType.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        for (int i = 0; i < NewSubDbSetTypeProperties.Length; i++)
                        {
                            PropertyInfo p = NewSubDbSetTypeProperties[i];
                            if (p.Name == "ElementId")
                            {
                                //p.SetValue(NewSubDbSetType, ElementId, null);
                                break;
                            }
                        }                        
                        //nu dezelfde referentie in het specialtype veld van SubDbSet invullen                        
                        for (int i = 0; i < SubDbSetProperties.Length; i++)
                        {
                            PropertyInfo p = SubDbSetProperties[i];
                            if (p.Name == field.Name)
                            {
                                if (field.PropertyType == typeof(object))
                                {
                                    //p.SetValue(SubDbSet, ElementId + "<--->REFNUMBER", null);
                                    break;
                                }
                                else
                                {
                                    //p.SetValue(SubDbSet, ElementId, null);
                                    break;
                                }
                            }
                        }                        

                        //waarschijnlijk een speciaal type. Op naar EvaluateSpecialType dus 
                        EvaluateSpecialType(value, ref ElementId, TypeOfSubDbSet, NewSubDbSetType, DataAssembly, ListOfTableLists, GenericTypes);
                    }

                    //ElementId++;
                }                                      
            }

            //ok, nu kunnen we de boel in de tabel zetten. Eerst propertyinfo van de betreffende DbSet ophalen            
            String ndbst = SubDbSet.GetType().FullName;
            for (int i = 0; i < GenericTypes.Length; i++)
            {
                Type p = GenericTypes[i];
                if (p.FullName == ndbst)
                {
                    ListOfTableLists[i].Add(SubDbSet);
                    break;
                }
            }            
        }

        private void CopyToDatabase(List<IList> ListOfTableLists, string[] TableNames, Type[] GenericTypes, String ConnectionString)
        {
            //bulk alle data in de database
            var bulker = new SqlBulkCopy(ConnectionString);

            //deze property is een echte lifesaver, bij enorme hoeveelheden data kan je hiermee voorkomen dat de boel uittimed. 120 betekend dat ik ongv 120 keer de sample data kan opslaan, en dat is heel veel data.
            bulker.BulkCopyTimeout = 120;

            for (int i = 0; i < ListOfTableLists.Count; i++)
            {
                String TargetTableName = TableNames[i];
                Type GenericType = GenericTypes[i];
                var Data = ListOfTableLists[i];

                //zet de lijst om naar een DataTable
                var newdata = Util.ObtainDataTableFromIEnumerable(Data); //dit is nu overigens de zwaarste call in het hele gebeuren, mogelijke optimalisatie hier doen.

                bulker.DestinationTableName = TargetTableName+"s";

                //save de data naar de database
                bulker.WriteToServer(newdata);
            }

            bulker.Close();
            bulker = null;
        }       
        #endregion

        #region Genereer database
        private List<CodeTypeDeclaration> EvaluateTypeForTypes(Type ClassType, Type MainType, object ClassTypeContent = null, List<CodeTypeDeclaration> CTDList = null, List<Type> CheckedTypes = null, Boolean SkipFinalEval = false)
        {
            //deze functie genereert alleen voor speciale types een class, wat dat zijn de uiteindelijke tabellen. We willen geen tabellen genaamd String of object.

            //check of we op hoogste niveau zitten            
            if (CTDList == null)
            {                
                CTDList = new List<CodeTypeDeclaration>();
                CheckedTypes = new List<Type>();
            }
         
            //check of het speciaal type is, moeten we misschien een nieuwe klasse voor genereren, maar voor nu moet er een key naar toe, een int dus            
            
            //maak voor het huidige type alvast een CTD, deze komt verderop in de DbContext
            CodeTypeDeclaration targetClass = new CodeTypeDeclaration(ClassType.Name + "TFDB");
            targetClass.BaseTypes.Add(new CodeTypeReference(typeof(object)));
            targetClass.IsClass = true;
            //targetClass.IsPartial = true;
            targetClass.TypeAttributes = TypeAttributes.Public;
            
            //generate Id property via de Fieldhack. Hoofdletter F, want hij is mooi.            
            var cfield = new CodeMemberField
            {
                Attributes = MemberAttributes.Public | MemberAttributes.Final,
                Name = "Id",
                Type = new CodeTypeReference(typeof(Int32)),                
            };
                       
            cfield.Name += " {get;set;} //";
            
            targetClass.Members.Add(cfield);

            //voeg ElementId toe, gebruiken we later om de verwijzingen te regelen            
            var elementid = new CodeMemberField
            {
                    
                Attributes = MemberAttributes.Public | MemberAttributes.Final,
                Name = "ElementId",
                Type = new CodeTypeReference(typeof(int)),
            };
            elementid.Name += " {get;set;} //";

            //CodeAttributeDeclaration cad = new CodeAttributeDeclaration("Key");
            //elementid.CustomAttributes.Add(cad);

            targetClass.Members.Add(elementid);

            //ok, de ClassType word 1 klasse, de speciale types en lists worden een verwijzing
            //FieldInfo[] fields = ClassType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
            PropertyInfo[] fields = ClassType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);

            foreach (PropertyInfo field in fields)
            {           
                //per field moet er een property komen in de class voor het huidige type
                String fieldname = field.Name;
                if (field.Name == "interface" || field.Name == "ref")
                    fieldname = "@" + field.Name;

                if (field.Name == "id")
                    fieldname = "idField";

                //create empty property
                CodeMemberField property;

                object[] atts = Assembly.GetAssembly(field.PropertyType).GetCustomAttributes(typeof(AssemblyProductAttribute), false);

                if (typeof(IEnumerable).IsAssignableFrom(field.PropertyType) && field.PropertyType != typeof(String))
                {
                    //lijst, kan zijn dat voor het type lijst een extra klasse moet worden gegenereerd
                    Type ty = field.PropertyType.GetGenericArguments()[0];

                    //dit type is dus mogelijk een speciaal type. Checken maar
                    atts = Assembly.GetAssembly(ty).GetCustomAttributes(typeof(AssemblyProductAttribute), false);

                    //ik moet eigenlijk ook checken of het een lijst is, een lijst in een lijst kan... niet zo flex maar moet kunnen
                    if (typeof(IEnumerable).IsAssignableFrom(ty) && field.PropertyType != typeof(String))
                    {
                        //lijst in lijst, hoeven voorlopig niks te doen
                    }
                    else if (atts.Length != 0 && String.Equals(((AssemblyProductAttribute)atts[0]).Product, "Microsoft® .NET Framework") && field.PropertyType != typeof(object))
                    {
                        /* als de executie hier uitkomt is er dus een lijst met als generiek type een standaard microsoft type. er moet dus een tabel komen die standaard lijsten kan opslaan
                         * deze hoeft maar 1 keer gemaakt te worden, hier gaan alle standaard lijsten in.                        
                         */                       
                    }
                    else
                    {
                        if (!CheckedTypes.Contains(field.PropertyType))
                        {
                            //speciaal type, begin de analyse opnieuw.
                            List<CodeTypeDeclaration> a = EvaluateTypeForTypes(ty, MainType, ClassTypeContent, CTDList, CheckedTypes);
                            CheckedTypes.Add(field.PropertyType);
                        }
                    }

                    //juist-o. een lijst heeft dus een generiek type, de compiler snapt het (!) dat de speciale types eigenlijk helemaal nog niet bestaan, maar in de klasse zitten die we gaan compilen... Magisch.                                            
                    //Bijvoorbeeld: solutionTypeTFDB bestaat runtime helemaal nog niet, maar pas na het compileren van het geheel. En toch werkt dit!
                 
                    //eerst moeten we het generieke type van deze lijst kapen. als die er is natuurlijk.  //TODO: wat als er geen generiek type is?               
                    Type t = field.PropertyType.GetGenericArguments()[0];
                    String generictype;
                    
                    object[] attsa = Assembly.GetAssembly(t).GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                    if(String.Equals(((AssemblyProductAttribute)attsa[0]).Product, "Microsoft® .NET Framework"))
                        generictype = "System.Collections.Generic.ICollection<MicrosoftTypeList>";
                    else
                        generictype = "System.Collections.Generic.ICollection<" + t.FullName + "TFDB>";

                    property = new CodeMemberField
                    {                        
                        Attributes = MemberAttributes.Public | MemberAttributes.Final, // deze moet virtual worden, maar werkt dus niet om de een of andere reden
                        Name = fieldname,
                        Type = new CodeTypeReference(generictype) // -> een lijst met het type van het object wat er in gaat. 
                    };

                    
                    property.Name += " {get;set;} //";
                    property.Comments.Add(new CodeCommentStatement("Reference to List<" + field.PropertyType.ToString() + ">"));
                    targetClass.Members.Add(property);
                }
                else if (atts.Length != 0 && String.Equals(((AssemblyProductAttribute)atts[0]).Product, "Microsoft® .NET Framework"))
                {
                    //standaard type, genereer standaard property met het type want dit type is waarschijnlijk bekend in de DB omgeving.

                    if (field.PropertyType == typeof(object))
                    {
                        property = new CodeMemberField
                        {
                            Attributes = MemberAttributes.Public | MemberAttributes.Final,
                            Name = fieldname,
                            Type = new CodeTypeReference(typeof(String))
                        };

                        property.Name += " {get;set;} //";
                        targetClass.Members.Add(property);
                    }
                    else
                    {
                        property = new CodeMemberField
                        {
                            Attributes = MemberAttributes.Public | MemberAttributes.Final,
                            Name = fieldname,
                            Type = new CodeTypeReference(field.PropertyType)
                        };

                        property.Name += " {get;set;} //";
                        targetClass.Members.Add(property);
                    }
                }
                else
                {
                    //speciaal type, voor dit speciale type moet nog een klasse worden gegenereerd

                    if (!CheckedTypes.Contains(field.PropertyType) && field.PropertyType != typeof(object))
                    {
                        //begin de analyse opnieuw met het type van de huidige field. 
                        List<CodeTypeDeclaration> a = EvaluateTypeForTypes(field.PropertyType, MainType, ClassTypeContent, CTDList, CheckedTypes);
                        CheckedTypes.Add(field.PropertyType);
                    }

                    if (field.PropertyType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Length == 0)
                    {
                        //object zonder velden
                        property = new CodeMemberField
                        {
                            Attributes = MemberAttributes.Public | MemberAttributes.Final,
                            Name = fieldname,
                            Type = new CodeTypeReference(field.PropertyType)
                        };                    
                    }
                    else
                    {                                      
                        property = new CodeMemberField
                        {
                            Attributes = MemberAttributes.Public | MemberAttributes.Final,
                            Name = fieldname,
                            Type = new CodeTypeReference(field.PropertyType.Name+"TFDB")
                            //Type = new CodeTypeReference(typeof(int))
                        };

                        property.Name += " {get;set;} //";
                        property.Comments.Add(new CodeCommentStatement("Reference to " + field.PropertyType.ToString()));
                    }

                    targetClass.Members.Add(property);
                }
            }

            //voeg de targetclass toe aan de CTDList.
            CTDList.Add(targetClass);

            if (ClassType == MainType && !SkipFinalEval)
            {
                //nu moeten we naar de content kijken of er niet stiekem speciale types zijn die als objecten staan in het hoofdtype. anders krijgen we straks dat tabellen niet worden gevonden voor een object
                List<Type> remainder = EvaluateContentForTypes(CheckedTypes, ClassTypeContent, ClassType);

                //na het uitvoeren van de functie is er lijst met een aantal types waarvan nog geen tabellen zijn. Gooi deze types opnieuw in deze functie en CTDList wordt aangevuld.
                for (int i = 0; i < remainder.Count; i++)
                    CTDList = EvaluateTypeForTypes(remainder[i], remainder[i], ClassTypeContent, CTDList, CheckedTypes, true);
            }
            
            return CTDList;
        }

        private List<Type> EvaluateContentForTypes(List<Type> CheckedTypes, object ClassTypeContent, Type ClassType, List<Type> GenerateClassList = null)
        {
            //deze functie doorzoekt de inhoud naar types die nodig niet voorkomen op CheckedTypes, hier komt een berking naar voren, want bij andere inhoud kunnen er andere types zijn en kan het dus alsnog mis gaan

            //FieldInfo[] fields = ClassType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
            PropertyInfo[] fields = ClassType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);

            if(GenerateClassList == null)
                GenerateClassList = new List<Type>();

            foreach (PropertyInfo field in fields)
            {
                //per field kijken wat het type van de value is

                var value = field.GetValue(ClassTypeContent, null);                

                if (value == null || value.GetType() == typeof(object)) { }
                else
                {
                    //doorgaan als het een object is, anders doorgaan. Voor object willen we geen tabel
                    Type TypeOfValue = value.GetType();

                    object[] atts = Assembly.GetAssembly(TypeOfValue).GetCustomAttributes(typeof(AssemblyProductAttribute), false);

                    if (value is IEnumerable && TypeOfValue != typeof(String) && (atts.Length != 0 && String.Equals(((AssemblyProductAttribute)atts[0]).Product, "Microsoft® .NET Framework")))
                    {
                        //checken of het gaat om een speciaal type
                        if (!CheckedTypes.Contains(TypeOfValue) && (atts.Length != 0 && !String.Equals(((AssemblyProductAttribute)atts[0]).Product, "Microsoft® .NET Framework")))
                        {
                            CheckedTypes.Add(TypeOfValue);
                            GenerateClassList.Add(TypeOfValue);
                        }

                        foreach (object o in (IEnumerable)value)
                        {
                            object[] atts2 = Assembly.GetAssembly(o.GetType()).GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                            if (atts2.Length != 0 && !String.Equals(((AssemblyProductAttribute)atts2[0]).Product, "Microsoft® .NET Framework"))
                                GenerateClassList = EvaluateContentForTypes(CheckedTypes, o, o.GetType(), GenerateClassList);
                        }
                    }
                    else if (atts.Length != 0 && String.Equals(((AssemblyProductAttribute)atts[0]).Product, "Microsoft® .NET Framework") && TypeOfValue != typeof(object))
                    {
                        //standaard type, niks doen
                    }
                    else
                    {
                        //speciaal type, staat ie al op CheckedTypes?
                        if (!CheckedTypes.Contains(TypeOfValue)  && (atts.Length != 0 && !String.Equals(((AssemblyProductAttribute)atts[0]).Product, "Microsoft® .NET Framework")))
                        {
                            //nee, het staat nog niet op de checkedlist, we moeten dus verdere tabellen genereren.
                            CheckedTypes.Add(TypeOfValue);
                            GenerateClassList.Add(TypeOfValue);
                        }

                        GenerateClassList = EvaluateContentForTypes(CheckedTypes, value, TypeOfValue, GenerateClassList);
                    }
                }
            }

            return GenerateClassList;
        }

        private object[] GenerateTablesForType(Type RecursionType, object RecursionTypeContent, Boolean IncludeGeneratedContextInProject)
        {
            //eerste stap is om een model te bouwen waarin de database komt, de DbContext die wordt gebruikt om een database te genereren.
            String raond = Util.GenerateRandomString(10);
            String outputFileName = "generated_model_" + raond + "_" + RecursionType.Name + ".cs";
            String outputFileNameExt = "generated_model_" + raond + "_" + RecursionType.Name + ".exe";

            //de targetUnit is de hoofdunit die uiteindelijk wordt gebruikt om de class te compileren. Hierin komen alle DbSets en de DbContext zelf
            CodeCompileUnit targetUnit = new CodeCompileUnit();
            System.CodeDom.CodeNamespace samples = new System.CodeDom.CodeNamespace(this.GetType().Namespace);

            //voeg using statements toe
            samples.Imports.Add(new CodeNamespaceImport("System"));
            samples.Imports.Add(new CodeNamespaceImport("System.Data"));
            samples.Imports.Add(new CodeNamespaceImport("System.Data.Entity"));
            samples.Imports.Add(new CodeNamespaceImport("System.ComponentModel.DataAnnotations"));
            
            //analyseer het type
            List<CodeTypeDeclaration> ctdlist = EvaluateTypeForTypes(RecursionType, RecursionType, RecursionTypeContent);

            //de lijst ctdlist bevat alle DbSets, wat uiteindelijk de tabellen gaan worden. Nu moet de DbContext er nog in.
            CodeTypeDeclaration dbcontext = new CodeTypeDeclaration();

            dbcontext.BaseTypes.Add(new CodeTypeReference(typeof(DbContext)));
            
            //geef de context een ietwat random naam, om te voorkomen dat de database al bestaat. zou vervelend zijn.             
            //Dit moet in de toekomst anders, voor als er meer data in dezelfde database moet komen.
            String contextname = "generated_" + Util.GenerateRandomString(10) + "_" + RecursionType.Name + "Context";
            dbcontext.Name = contextname;

            //voeg constructor toe aan de DbContext, deze moet de connnectie string hebben naar de DB die we zo gaan maken. 
            CodeConstructor constructor = new CodeConstructor();
            constructor.Attributes = MemberAttributes.Public;

            //dit is de connectionstring, het bevat de naam die we aan de database gaan geven.
            String connectionstring = "Data Source=(local)\\SQLEXPRESS;Initial Catalog=" + contextname + ";Integrated Security=True;Pooling=False;";
            constructor.BaseConstructorArgs.Add(new CodePrimitiveExpression(connectionstring.ToString()));

            //voeg constructor toe aan Members.
            dbcontext.Members.Add(constructor);

            //voeg een extra tabel/dbset toe om microsoft lijsten in op te slaan. 
            CodeTypeDeclaration ListTableClass = new CodeTypeDeclaration("MicrosoftTypeList");
            ListTableClass.BaseTypes.Add(new CodeTypeReference(typeof(object)));

            //genereer Id property via de Fieldhack. Hoofdletter F, want hij is mooi. Dit is noodzakelijk, want je kan geen automatic property toevoegen met CodeDOM.           
            var idfield = new CodeMemberField
            {
                Attributes = MemberAttributes.Public | MemberAttributes.Final,
                Name = "Id",
                Type = new CodeTypeReference(typeof(Int32)),
            };

            //in de volgende regel zit de daadwerkelijke hack, we voegen de automatic property toe als string aan de naam en zorgen met de comment slashes dat er geen ; achter komt de staan. 
            //Dit compilereert als automatic property maar is in feite een field. Handig. 
            idfield.Name += " {get;set;} //";
            ListTableClass.Members.Add(idfield);

            //voeg ElementId toe, gebruiken we later om de verwijzingen te regelen            
            var elementid = new CodeMemberField
            {                
                Attributes = MemberAttributes.Public | MemberAttributes.Final,
                Name = "ElementId",
                Type = new CodeTypeReference(typeof(Int32)),
            };

            //wederom fieldhack doen, dit gebeurt bij alle properties dus. 
            elementid.Name += " {get;set;} //";
            ListTableClass.Members.Add(elementid);

            //voeg value veld toe, om de lijst value in op te slaan
            var value = new CodeMemberField
            {
                Attributes = MemberAttributes.Public | MemberAttributes.Final,
                Name = "Value",
                Type = new CodeTypeReference(typeof(String)),
            };
            value.Name += " {get;set;} //";
            ListTableClass.Members.Add(value);

            ctdlist.Add(ListTableClass);           

            //nu elke CodeTypeDeclaration in ctdlist toevoegen aan onze DbContext klasse als property
            foreach (CodeTypeDeclaration targetClass in ctdlist)
            {                
                CodeTypeReference a = new CodeTypeReference(typeof(DbSet<>));
                a.TypeArguments.Add(targetClass.Name);

                var cfield = new CodeMemberField
                {
                    Attributes = MemberAttributes.Public | MemberAttributes.Final,
                    Name = targetClass.Name,
                    Type = a,
                };

                cfield.Name += " {get;set;} //";

                dbcontext.Members.Add(cfield);
            }

            //voeg startpunt toe, een static Main, anders compileert het niet. Dit is trouwens beetje lastig voor later, want 2 mains in een prog kan niet!! Deze moet dus eigenlijk niet. 
            CodeEntryPointMethod start = new CodeEntryPointMethod();
            start.Name = "Apekop";
            dbcontext.Members.Add(start);

            //voeg een GetType methode toe, voor het verkrijgen van de speciale types en voor het gebruik bij type vergelijking.         
            CodeMemberMethod method1 = new CodeMemberMethod();
            method1.Name = "GetTypeExt";
            method1.Attributes = MemberAttributes.Public;
            method1.ReturnType = new CodeTypeReference("System.Type");
            method1.Statements.Add(new CodeMethodReturnStatement(new CodeArgumentReferenceExpression("this.GetType()")));

            dbcontext.Members.Add(method1);

            ctdlist.Add(dbcontext);                     
            
            //handigheidje, staat het hoofdtype bovenaan en het diepste type onderaan, is verder niet echt nodig
            ctdlist.Reverse();

            //voeg elke CTD toe aan de samples namespace
            foreach (CodeTypeDeclaration targetClass in ctdlist)
                samples.Types.Add(targetClass);

            //laatste move, voeg de namespace toe aan de compileunit
            targetUnit.Namespaces.Add(samples);

            //maak een CSharpProvider voor het compilen
            CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");

            //maak options voor de compiler
            CodeGeneratorOptions options = new CodeGeneratorOptions();
            options.BracingStyle = "C";

            //schrijf naar cs file
            using (StreamWriter sourceWriter = new StreamWriter(outputFileName))
            {
                provider.GenerateCodeFromCompileUnit(
                    targetUnit, sourceWriter, options);
            }

            //voeg referenties toe, bijvoorbeeld EntityFramework hebben we nodig vanwege de DbContext
            CompilerParameters cp = new CompilerParameters();
            cp.GenerateExecutable = true;
            cp.GenerateInMemory = false;
            cp.ReferencedAssemblies.Add("System.dll");
            cp.ReferencedAssemblies.Add("EntityFramework.dll");
            cp.ReferencedAssemblies.Add("System.Data.Entity.dll");
            cp.ReferencedAssemblies.Add("System.Data.dll");
            cp.ReferencedAssemblies.Add("System.Core.dll");
            cp.ReferencedAssemblies.Add("System.ComponentModel.DataAnnotations.dll");
            cp.TreatWarningsAsErrors = false;
            cp.OutputAssembly = outputFileNameExt;

            //compile het hele ding naar een assembly
            CompilerResults cr = provider.CompileAssemblyFromDom(cp, targetUnit);            


            #region compiler errors
            //compiler errors? printen maar
            if (cr == null || cr.Errors.Count > 0)
            {
                for (int i = 0; i < cr.Errors.Count; i++)
                {
                    Console.WriteLine(cr.Errors[i]);
                }
                Console.ReadLine();
                
                throw new Exception("Epic fail. Something is amiss. Please sort it out and try again");
                
            }
            #endregion

            /*
             * Als de bool IncludeGeneratedContextInProject op true staat wil de gebruiker de gegenereerde file toevoegen aan het project. 
             * Dit kan link zijn, want de types in die file kunnen al ergens anders bestaan (zoals ik heb met xmltest namespace) problematisch dus. Hoe los ik dit op?
             * Als de gebruiker echter alleen een db wil bouwen dan kan dat ook. Heb je die context niet eens voor nodig. En de context is er, dus kan ook handmatig ingevoegd worden 
             * onder een andere naam. Het gaat erom dat de gebruiker niet die context zelf hoeft te maken. 
             */

            String fullpath = System.Reflection.Assembly.GetAssembly(this.GetType()).Location;
            String dir = Path.GetDirectoryName(fullpath);

            if (IncludeGeneratedContextInProject)
            {
                //voeg toe aan project, dit is een lastig gebeuren want dit betekend dat we het DTE object van de IDE moeten gebruiken.                 
                EnvDTE80.DTE2 dteC;

                //vind pad naar deze solution
                String path = Environment.CurrentDirectory;
                List<String> split = path.Split(new char[] { '\\' }).ToList<String>();
                int end = split.Count;
                split.RemoveRange(end - 3, 3);                

                String newpath = String.Join("\\", split);

                //vanwege de volgende regel werkt deze functie alleen maar met VS 10. moet een versie selectie ding komen wil dit werken met alle VS versies. 
                dteC = (EnvDTE80.DTE2)System.Runtime.InteropServices.Marshal.GetActiveObject("VisualStudio.DTE.10.0");
                EnvDTE.Project proj = dteC.Solution.Projects.Item(1);
                ProjectItem addclass = proj.ProjectItems.AddFromFileCopy(dir + "\\" + outputFileName);
                
                proj.Save( newpath + "\\a2_rdbgenerator2\\a2_rdbgenerator2.csproj"); //dit moet via een property ergens. maar kan het nu even niet vinden
            }

            //laad de zojuist gecompileerde assembly met daarin de DbContext
            Assembly contextassembly = Assembly.LoadFrom(dir + "\\" + outputFileNameExt);
            

            //gebruik de superhandige Activator class om een instantie te maken (via reflection dus) van de zojuist geladen DbContext
            DbContext databasecontext = (DbContext)Activator.CreateInstance(contextassembly.FullName, "genericdbgenerator." + contextname).Unwrap();

            //nog niet helemaal klaar, pak alle properties van de DbContext (DbSets)
            Type TypeOfContext = Activator.CreateInstance(contextassembly.FullName, "genericdbgenerator." + contextname).Unwrap().GetType();
            PropertyInfo[] DbSets = TypeOfContext.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(u => u.PropertyType.IsGenericType == true).ToArray();
                     
            //deze lijst gaat verderop een grote rol spelen, dit is namelijk de lijst waarin alle content gaat komen en die aan het einde SqlBulkCopy in gaat. 
            List<IList> ListList = new List<IList>();            
            String[] TableNames = new String[DbSets.Length];           
            Type[] GenericTypes = new Type[DbSets.Length];            

            for (int i = 0; i < DbSets.Length; i++)
            {
                //nu per propertyinfo een lijst maken voor ListList
                PropertyInfo DbSetProperty = DbSets[i];
                Type DbSetPropertyGenericType = DbSetProperty.PropertyType.GetGenericArguments()[0];

                //nu moeten we voor elk generiek type (tabel) een lijst met dat generieke type maken. 
                var GenericListType = typeof(List<>).MakeGenericType(DbSetPropertyGenericType);
                var GenericList = Activator.CreateInstance(GenericListType);
                
                ListList.Add(GenericList as IList);

                PluralizationService plz = PluralizationService.CreateService(CultureInfo.GetCultureInfo("en-us"));
                
                TableNames[i] = "dbo." + DbSetPropertyGenericType.Name;
                GenericTypes[i] = DbSetPropertyGenericType;
            }            
            
            //en dan nu (tromgeroffel) het creëren van de database zelf, met de veelbesproken Create() method.             
            databasecontext.Database.Create();
            
            //zo. Nu is er een database voor het opgegeven type, een DbContext, een lijst met lijsten voor elk type in het opgegeven type, de dbSets, de tabelnamen enz. Alles wat we nodig hebben om op te gaan slaan
            object[] retur = new object[] { contextassembly, connectionstring, ListList, TableNames, GenericTypes, databasecontext, DbSets };
            return retur;
        }        
       

        #endregion

        private void button1_Click(object sender, EventArgs e)
        {
            //dit word even mijn test functie
            
            generated_ENKSAVGONW_qpfContext org = new generated_ENKSAVGONW_qpfContext();

            org.Database.CreateIfNotExists();
            
            qpfTFDB QPF = new qpfTFDB();

            QPF.ElementId = 1;            

            solutionTypeTFDB SOL1 = new solutionTypeTFDB();           
            solutionTypeTFDB SOL2 = new solutionTypeTFDB();
            solutionTypeTFDB SOL3 = new solutionTypeTFDB();

            solutionTypeTFDB SOL4 = new solutionTypeTFDB();
            solutionTypeTFDB SOL5 = new solutionTypeTFDB();
            solutionTypeTFDB SOL6 = new solutionTypeTFDB();


            //deze ElementIds linken naar het solutions veld van de qpfTFDB hierboven.
            SOL1.ElementId = 1; 
            SOL2.ElementId = 1;
            SOL3.ElementId = 1; //qpf.id = 1

            SOL4.ElementId = 2;
            SOL5.ElementId = 2;
            SOL6.ElementId = 2; //qpf.id = 2

            //wat sample data
            SOL1.codb = "1";
            SOL2.codb = "2";
            SOL3.codb = "3";

            SOL4.codb = "1";
            SOL5.codb = "2";
            SOL6.codb = "3";

            qpfKernelqkbTFDB kernel = new qpfKernelqkbTFDB();
            kernel.Value = "De Kernel";
            kernel.ElementId = 1;

            //org.qpfKernelqkbTFDB.Add(kernel);
            
            org.solutionTypeTFDB.Add(SOL1);
            org.solutionTypeTFDB.Add(SOL2);
            org.solutionTypeTFDB.Add(SOL3);

            //dit wil nog niet
            //org.solutionTypeTFDB.Add(SOL4);
            //org.solutionTypeTFDB.Add(SOL5);
            //org.solutionTypeTFDB.Add(SOL6);

            org.qpfTFDB.Add(QPF);
            
            org.SaveChanges();             
        }

        private void button2_Click(object sender, EventArgs e)
        {
            generated_ENKSAVGONW_qpfContext org = new generated_ENKSAVGONW_qpfContext();

            var resultaat = from c in org.qpfTFDB select c;         
  

            //

        }

        #region NOTITIYA
        /*
         * virtual is blijkbaar een lazy loading verhaal, heeft verder geen invloed op relations
         * virtual kan niet op normale properties, maar als er getters en setters zijn werkt het wel
         * 1:1 is natuurlijk makkelijk, kan op basis van het type van de property, maar 1:m of 1:n is wat lastiger, moet ik schijnbaar iets wat de 'Fluent API' heet gebruiken
         * convention-over-configuration gaat uit van de goedheid van de mens, is ook de manier waarop EF werkt, gaat uit dat de ontwikkelaar (ik) de conventies aanhoud, wat natuurlijk niet echt werkt als ik ze niet ken. ffs.
         * Fluent api om relations te mappen, werkt echt.
         * 
         * juistem, probleem: ik weet bij object niet wat het type is, dus ik kan geen relations leggen. het type weet ik pas als ik de data heb, en die kan ik pas ophalen als de relations zijn gelegd.
         * http://stackoverflow.com/questions/4398583/mapping-properties-to-differently-named-foreign-key-fields-in-entity-framework
         * 
         * de foreign key is qpf.id naar solution.elementid
         * de ? bij int? foreign key lijkt uit te maken, iets met multiplicity
         */
        #endregion
    }        
}
