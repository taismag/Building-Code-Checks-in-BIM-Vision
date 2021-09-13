using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Artigo_36º_PDM
{
    using BIMVision;
    using System.Configuration;
    using System.Diagnostics.Eventing.Reader;
    using System.Security.Permissions;
    using System.Security.Principal;
    using System.Threading;

    public class Art36ºPDM : Plugin
    {
        private ApiWrapper api;

        private OBJECT_ID allId;
        private int button1;

        public override void GetPluginInfo(ref PluginInfo info)
        {
            info.name = "Artigo 36º PDM";
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
            api.SetButtonText(button1, "PDM Artigo 36º", "Test");

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


        public Edge[] ArestasObjeto(OBJECT_ID objeto)
        {
            Edge[] arestas = new Edge[12];

            //List<Edge> arestas = new List<Edge>();

            var geometria = api.FirstGeometry(objeto);

            if (geometria == true)
            {
                api.GetGeometry();
                arestas = api.GetGeometryEdges();
                //int tamanho = arestas.Length;
            }
            return arestas;
        }

        private void buttonClick()
        {
            // metodologia usada: medir a distancia da frente dos edificios ate a rua frontal e ver as posiçoes dos edificios

            List<double> todas_distancias_parede = new List<double>();
            List<double> todas_distancias_ed_d = new List<double>();
            List<double> todas_distancias_ed_e = new List<double>();

            string nome_rua;
            string nome_edificio;

            double distancia_1;
            double distancia_2;
            double distancia_3;
            double distancia_4;

            double menor_distancia_parede;
            double menor_distancia_ed_d;
            double menor_distancia_ed_e;


            Vertex vertice1_rua;
            Vertex vertice2_rua;

            Vertex vertice1_parede;
            Vertex vertice2_parede;

            Vertex vertice1_ed;
            Vertex vertice2_ed;

            Vertex closestp1;
            Vertex closestp2;
            Vertex closestp3;
            Vertex closestp4;


            //Filtro de objetos Rua
            List<OBJECT_ID> ruas = new List<OBJECT_ID>();
            ruas = FiltrarElementos("IfcSlab");

            // Filtro paredes
            List<OBJECT_ID> paredes = new List<OBJECT_ID>();
            paredes = FiltrarElementos("IfcWallStandardCase");

            //Filtro edificios
            List<OBJECT_ID> edificios = new List<OBJECT_ID>();
            edificios = FiltrarElementos("IfcBuilding");


            foreach (OBJECT_ID rua in ruas)
            {
                nome_rua = ValorPropriedade2(rua, "Name");

                if (nome_rua == "Arruamento frontal")
                {
                    int t = ArestasObjeto(rua).Length;

                    Edge[] arestas_rua_frontal = new Edge[t];

                    arestas_rua_frontal = ArestasObjeto(rua);

                    foreach (Edge aresta_r in arestas_rua_frontal)
                    {
                        vertice1_rua = aresta_r.v1;
                        vertice2_rua = aresta_r.v2;

                        foreach (OBJECT_ID parede in paredes)
                        {
                            string descricao = ValorPropriedade2(parede, "Descrição de fachada");
                            bool alinhamento = ValorPropriedade2(parede, "Alinhamento");

                            if (descricao == "Fachada frontal" && alinhamento == true)
                            {
                                int tamanho = ArestasObjeto(parede).Length;

                                Edge[] arestas_parede_frontal = new Edge[tamanho];

                                arestas_parede_frontal = ArestasObjeto(parede);

                                foreach (Edge aresta_p in arestas_parede_frontal)
                                {
                                    vertice1_parede = aresta_p.v1;
                                    vertice2_parede = aresta_p.v2;

                                    //FindDistanceToSegment(vertice, endline1, endline2, out closest)

                                    distancia_1 = FindDistanceToSegment(vertice1_rua, vertice1_parede, vertice2_parede, out closestp1);
                                    todas_distancias_parede.Add(distancia_1);

                                    distancia_2 = FindDistanceToSegment(vertice2_rua, vertice1_parede, vertice2_parede, out closestp2);
                                    todas_distancias_parede.Add(distancia_2);

                                    distancia_3 = FindDistanceToSegment(vertice1_parede, vertice1_rua, vertice2_rua, out closestp3);
                                    todas_distancias_parede.Add(distancia_3);

                                    distancia_4 = FindDistanceToSegment(vertice2_parede, vertice1_rua, vertice2_rua, out closestp4);
                                    todas_distancias_parede.Add(distancia_4);
                                }
                            }
                        }
                    }
                }
            }

            menor_distancia_parede = Math.Round(todas_distancias_parede.Min(), 3);

            // edificio direito:

            foreach (OBJECT_ID rua in ruas)
            {
                nome_rua = ValorPropriedade2(rua, "Name");

                if (nome_rua == "Arruamento frontal")
                {
                    int t = ArestasObjeto(rua).Length;

                    Edge[] arestas_rua_frontal = new Edge[t];

                    arestas_rua_frontal = ArestasObjeto(rua);

                    foreach (Edge aresta_r in arestas_rua_frontal)
                    {
                        vertice1_rua = aresta_r.v1;
                        vertice2_rua = aresta_r.v2;

                        foreach (OBJECT_ID edificio in edificios)
                        {
                            nome_edificio = ValorPropriedade2(edificio, "Name");

                            if (nome_edificio == "Edifício Adjacente D")
                            {
                                int tamanho = ArestasObjeto(edificio).Length;

                                Edge[] arestas_edificio_direito = new Edge[tamanho];

                                arestas_edificio_direito = ArestasObjeto(edificio);

                                foreach (Edge aresta_ed_d in arestas_edificio_direito)
                                {
                                    vertice1_ed = aresta_ed_d.v1;
                                    vertice2_ed = aresta_ed_d.v2;

                                    //FindDistanceToSegment(vertice, endline1, endline2, out closest)

                                    distancia_1 = FindDistanceToSegment(vertice1_rua, vertice1_ed, vertice2_ed, out closestp1);
                                    todas_distancias_ed_d.Add(distancia_1);

                                    distancia_2 = FindDistanceToSegment(vertice2_rua, vertice1_ed, vertice2_ed, out closestp2);
                                    todas_distancias_ed_d.Add(distancia_2);

                                    distancia_3 = FindDistanceToSegment(vertice1_ed, vertice1_rua, vertice2_rua, out closestp3);
                                    todas_distancias_ed_d.Add(distancia_3);

                                    distancia_4 = FindDistanceToSegment(vertice2_ed, vertice1_rua, vertice2_rua, out closestp4);
                                    todas_distancias_ed_d.Add(distancia_4);
                                }
                            }
                        }
                    }
                }
            }
            menor_distancia_ed_d = Math.Round(todas_distancias_ed_d.Min(), 3);


            // edificio esquerdo:

            foreach (OBJECT_ID rua in ruas)
            {
                nome_rua = ValorPropriedade2(rua, "Name");

                if (nome_rua == "Arruamento frontal")
                {
                    int t = ArestasObjeto(rua).Length;

                    Edge[] arestas_rua_frontal = new Edge[t];

                    arestas_rua_frontal = ArestasObjeto(rua);

                    foreach (Edge aresta_r in arestas_rua_frontal)
                    {
                        vertice1_rua = aresta_r.v1;
                        vertice2_rua = aresta_r.v2;

                        foreach (OBJECT_ID edificio in edificios)
                        {
                            nome_edificio = ValorPropriedade2(edificio, "Name");

                            if (nome_edificio == "Edifício Adjacente E")
                            {
                                int tamanho = ArestasObjeto(edificio).Length;

                                Edge[] arestas_edificio_esquerdo = new Edge[tamanho];

                                arestas_edificio_esquerdo = ArestasObjeto(edificio);

                                foreach (Edge aresta_ed in arestas_edificio_esquerdo)
                                {
                                    vertice1_ed = aresta_ed.v1;
                                    vertice2_ed = aresta_ed.v2;

                                    //FindDistanceToSegment(vertice, endline1, endline2, out closest)

                                    distancia_1 = FindDistanceToSegment(vertice1_rua, vertice1_ed, vertice2_ed, out closestp1);
                                    todas_distancias_ed_e.Add(distancia_1);

                                    distancia_2 = FindDistanceToSegment(vertice2_rua, vertice1_ed, vertice2_ed, out closestp2);
                                    todas_distancias_ed_e.Add(distancia_2);

                                    distancia_3 = FindDistanceToSegment(vertice1_ed, vertice1_rua, vertice2_rua, out closestp3);
                                    todas_distancias_ed_e.Add(distancia_3);

                                    distancia_4 = FindDistanceToSegment(vertice2_ed, vertice1_rua, vertice2_rua, out closestp4);
                                    todas_distancias_ed_e.Add(distancia_4);
                                }
                            }
                        }
                    }
                }
            }
            menor_distancia_ed_e = Math.Round(todas_distancias_ed_e.Min(), 3);


            //ver qual a menor:


        }

    }
}


/*  Regras de modelação para este artigo:
 *  
 *  ******************************************************************     
 *  Geral:
 *      Modelar t
 *  
 *  */