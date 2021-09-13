using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace Distancias
{
    using BIMVision;
    using System.Diagnostics.Eventing.Reader;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Security.Permissions;
    using System.Threading;

    using g3;
    using gs;
    
    public class Distancia : Plugin
    {

        private ApiWrapper api;

        private OBJECT_ID allId;
        private int button1;

        public override void GetPluginInfo(ref PluginInfo info)
        {
            info.name = "Distancias";
            info.producer = "Company";
            info.www = "www.company.com";
            info.email = "company@company.com";
            info.description = "C# basics example";
            //info.help_directory = "";
        }

        public override byte[] GetPluginKey()
        {
            return null;
        }

        public override void OnLoad(PLUGIN_ID pid, bool registered, IntPtr viewerHwnd)
        {
            api = new ApiWrapper(pid);

            api.OnModelLoad(onModelLoad);

            button1 = api.CreateButton(0, buttonClick);
            api.SetButtonText(button1, "Distancias", "Test");

        }

        public override void OnCallLimit()
        {
        }

        public override void OnUnload()
        {
        }

        public override void OnGuiColorsChange()
        {
        }

        private void onModelLoad()
        {
            allId = api.GetAllObjectsId();
        }



        public List<OBJECT_ID> FiltrarElementos(string classe_IFC)
        {
            //int n = 0;
            //OBJECT_ID[] objetos_filtrados = new OBJECT_ID[n];

            List<OBJECT_ID> objetos_filtrados = new List<OBJECT_ID>();

            //Pegar todos os objetos
            var objects = api.GetAllObjects();

            //Filtro de objetos 

            foreach (OBJECT_ID obj in objects)
            {
                var informaçoes = api.GetObjectInfo(obj);

                var entidade_ifc = informaçoes.ifc_entity_name;

                if (entidade_ifc.ToString() == classe_IFC)
                {
                    objetos_filtrados.Add(obj);
                }
            }
            return objetos_filtrados;
        }


        public dynamic ValorPropriedade2(OBJECT_ID objeto, string propriedade)
        {
            var propriedades = api.GetObjectProperties(objeto, 0);

            int tipo_valor;
            dynamic valor_num;
            string valor_str;
            bool vb;
            dynamic resultado = null;

            //string str_resultado;

            foreach (BIMVision.ApiWrapper.PropertySetData p in propriedades)
            {
                if (p.name == propriedade)
                {
                    tipo_valor = p.value_type;

                    if (tipo_valor == 0)    //string
                    {
                        valor_str = p.value_str;
                        resultado = valor_str;
                    }

                    else if (tipo_valor == 1)   //double
                    {
                        valor_num = p.value_num;
                        resultado = valor_num;
                    }

                    else if (tipo_valor == 2)   //int
                    {
                        valor_num = p.value_num;
                        resultado = valor_num;

                    }

                    else if (tipo_valor == 3)   //bool
                    {
                        valor_num = p.value_num;
                        vb = Convert.ToBoolean(valor_num);
                        resultado = vb;
                    }
                }
            }
            return resultado;
            //str_resultado = resultado.ToString();
        }

        // This is not right yet... Need to find out the difference between point and vertex and how to work with them:

        // Calculate the distance between
        // point pt and the segment p1 --> p2.
        private double FindDistanceToSegment(Vertex pt, Vertex p1, Vertex p2, out Vertex closest)
        {
            //p1.z = p2.z = pt.z = 0;

            double dx = p2.x - p1.x;
            double dy = p2.y - p1.y;
            if ((dx == 0) && (dy == 0))
            {
                // It's a point not a line segment.
                closest = p1;
                dx = pt.x - p1.x;
                dy = pt.y - p1.y;
                return Math.Sqrt(dx * dx + dy * dy);
            }

            // Calculate the t that minimizes the distance.
            double t = ((pt.x - p1.x) * dx + (pt.y - p1.y) * dy) /
                (dx * dx + dy * dy);

            // See if this represents one of the segment's
            // end points or a point in the middle.
            if (t < 0)
            {
                closest = new Vertex();
                closest.x = p1.x;
                closest.y = p1.y;
                closest.z = 0;
                dx = pt.x - p1.x;
                dy = pt.y - p1.y;
            }
            else if (t > 1)
            {
                closest = new Vertex();
                closest.x = p2.x;
                closest.y = p2.y;
                closest.z = 0;
                dx = pt.x - p2.x;
                dy = pt.y - p2.y;
            }
            else
            {
                closest = new Vertex();
                closest.x = p1.x + t * dx;
                closest.y = p1.y + t * dy;
                closest.z = 0;
                dx = pt.x - closest.x;
                dy = pt.y - closest.y;
            }

            return Math.Sqrt(dx * dx + dy * dy);
        }


//        private void buttonClick()
//        {
//            List<OBJECT_ID> paredes_laterais = new List<OBJECT_ID>();

//            //caso 1:
//            Vertex vertice1_ed;
//            Vertex vertice2_ed;
//            Vertex vertice_p_1;
//            Vertex vertice_p_2;
//            Vertex closestp1;

//            //caso 2:
//            Vertex vertice1_p;
//            Vertex vertice2_p;
//            Vertex vertice_ed_1;
//            Vertex vertice_ed_2;
//            Vertex closestp2;

//            double distancia_1;
//            double distancia_2;
//            double menor_distancia;

//            List<double> todas_distancias = new List<double>();

//            OBJECT_ID objeto1 = new OBJECT_ID();
//            OBJECT_ID objeto2 = new OBJECT_ID();

//            //Filtro de objeto 1 - IfcBuilding
//            List<OBJECT_ID> edificios = FiltrarElementos("IfcBuildingElementProxy");

//            //Filtrar objeto 2 - IfcWall
//            List<OBJECT_ID> paredes = FiltrarElementos("IfcWallStandardCase");



//            //distancia entre arestas do edificio e vertices da parede
//            foreach (OBJECT_ID edificio in edificios)
//            {
//                string j = ValorPropriedade2(edificio, "Name");
//                if (j == "Edifício 1D")
//                {
//                    objeto1 = edificio;

//                    var g = api.FirstGeometry(objeto1);
//                    if (g == true)
//                    {
//                        api.GetGeometry();
//                        Edge[] edges = api.GetGeometryEdges();

//                        foreach (Edge edge in edges)
//                        {
//                            vertice1_ed = edge.v1;
//                            vertice2_ed = edge.v2;

//                            //paredes:
//                            foreach (OBJECT_ID parede in paredes)
//                            {
//                                string i = ValorPropriedade2(parede, "Descrição de fachada");
//                                if (i == "Fachada lateral D")
//                                {
//                                    objeto2 = parede;

//                                    var g_parede = api.FirstGeometry(objeto2);
//                                    if (g_parede == true)
//                                    {
//                                        api.GetGeometry();
//                                        Edge[] edges_p = api.GetGeometryEdges();

//                                        foreach (Edge edge_p in edges_p)
//                                        {
//                                            vertice_p_1 = edge_p.v1;
//                                            vertice_p_2 = edge_p.v2;
                                            
//                                            distancia_1 = FindDistanceToSegment(vertice_p_1, vertice1_ed, vertice2_ed, out closestp1);
//                                            todas_distancias.Add(distancia_1);

//                                            distancia_2 = FindDistanceToSegment(vertice_p_2, vertice1_ed, vertice2_ed, out closestp1);
//                                            todas_distancias.Add(distancia_2);
//                                        }
//                                    }
//                                }
//                            }
//                        }
//                    }
//                }
//            }


//            //distancias entre arestas da parede e vertices do edificio
//            foreach (OBJECT_ID edificio in edificios)
//            {
//                string j = ValorPropriedade2(edificio, "Name");
//                if (j == "Edifício 1D")
//                {
//                    objeto1 = edificio;

//                    var g = api.FirstGeometry(objeto1);
//                    if (g == true)
//                    {
//                        api.GetGeometry();
//                        Edge[] edges = api.GetGeometryEdges();

//                        foreach (Edge edge in edges)
//                        {
//                            vertice_ed_1 = edge.v1;
//                            vertice_ed_2 = edge.v2;

//                            //paredes:
//                            foreach (OBJECT_ID parede in paredes)
//                            {
//                                string i = ValorPropriedade2(parede, "Descrição de fachada");
//                                if (i == "Fachada lateral D")
//                                {
//                                    objeto2 = parede;

//                                    var g_parede = api.FirstGeometry(objeto2);
//                                    if (g_parede == true)
//                                    {
//                                        api.GetGeometry();
//                                        Edge[] edges_p = api.GetGeometryEdges();

//                                        foreach (Edge edge_p in edges_p)
//                                        {
//                                            vertice1_p = edge_p.v1;
//                                            vertice2_p = edge_p.v2;

//                                            distancia_1 = FindDistanceToSegment(vertice_ed_1, vertice1_p, vertice2_p, out closestp2);
//                                            todas_distancias.Add(distancia_1);

//                                            distancia_2 = FindDistanceToSegment(vertice_ed_2, vertice1_p, vertice2_p, out closestp2);
//                                            todas_distancias.Add(distancia_2);
//                                        }
//                                    }
//                                }
//                            }
//                        }
//                    }
//                }
//            }

//            menor_distancia = todas_distancias.Min();
//            double valor_final = Math.Round(menor_distancia, 3);
//            api.MessageBox("distancia", valor_final.ToString(), 0);

//            //quando a parede é desenhada com as extremidades acabando e começando no lufgar errado, da um erro (pequeno)

//            try
//            {
//                api.Invalidate();
//            }
//            catch (BIMVision.DemoModeCallLimitException)
//            {
//            }

//        }
//    }
//}







//estrutura medir -nao sei usar ainda...
//Measure medir;
//medir.measure_type = MeasureType.mt_point_edge;



//OBJECT_ID[] objetos = api.GetMeasureObjects();
//foreach (OBJECT_ID o in objetos)
//{ api.MessageBox("objetos", o.ToString(), 0); }


//Bounds[] bounds = api.Get2DBounds(objetos, 0);

//api.GetMeasure();
//api.GetMeasureCount();
//api.SetMeasureType(5, 0);
//api.GetMeasureElements();



/*
 var prop1 = api.GetObjectProperties(objeto1, 0);
 foreach (BIMVision.ApiWrapper.PropertySetData p1 in prop1)
 {
     if (p1.name == "XDim")
     { api.MessageBox("dimx", p1.value_num.ToString(), 0); }
 }

 api.get_measure_count(objeto1)

     api.ItemCommand()

 var ob = api.GetTotalGeometryBounds();

 api.MessageBox("geometrua", api.FirstGeometry(objeto1).ToString(), 0);
 api.GetBounds(objeto1, );
 api.GetTotalGeometryBounds();
 api.MessageBox("oioio", api.GetGeometry().ToString(), 0);
 */
