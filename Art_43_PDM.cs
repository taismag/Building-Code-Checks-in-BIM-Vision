using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Artigo_43_PDM

//OBS: fazer regra de modelação para parede (inicio e fim). os comprimentos das laterais estão diferentes.
{
    using BIMVision;
    using System.Configuration;
    using System.Diagnostics.Eventing.Reader;
    using System.Security.Permissions;
    using System.Threading;
    public class Art_43_PDM : Plugin
    {
        private ApiWrapper api;
        private OBJECT_ID allId;
        private int button1;

        public override void GetPluginInfo(ref PluginInfo info)
        {
            info.name = "Art 43 PDM";
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
            api.SetButtonText(button1, "PDM Artigo 43º", "Test");

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


        // função para pegar o IfcName dos elementos (o que aparece em Element Specific no BIM Vision)
        public dynamic ValorIfcName(OBJECT_ID objeto)
        {
            var propriedades = api.GetProperties(objeto, 0); //usa o pset de numero 0 (propriedades que ficam em cima e não pertencem a pset nenhum

            var resultado = "";

            //api.MessageBox("contar", propriedades.Length.ToString() , 0);

            foreach (BIMVision.ApiWrapper.Property p in propriedades)
            {
                string nome = p.name;
                int number = p.nr;


                if (nome == "Name")
                {
                    var valor = p.value.value_str;

                    //api.SelectProperty(0, number, true); //o que fazer com isso?
                    resultado = valor;
                }
            }

            return resultado;
        }




        private void buttonClick()
        {
            List<OBJECT_ID> paredes = new List<OBJECT_ID>();
            paredes = FiltrarElementos("IfcWall");

            //List<OBJECT_ID> paredes_laterais = new List<OBJECT_ID>();

            List<OBJECT_ID> pisos = new List<OBJECT_ID>();
            pisos = FiltrarElementos("IfcBuildingStorey");

            List<OBJECT_ID> espaços = new List<OBJECT_ID>();
            espaços = FiltrarElementos("IfcSpace");

            List<double> todas_distancias = new List<double>();

            string nome_piso;
            string nome_espaço;

            double profundidade_max_RC = 35;
            double profundidade_max_acima_RC = 17.5;
            double profundidade = 0;

            double largura_pf = 0;
            double largura_pt = 0;

            Vertex vertice1_pf;
            Vertex vertice2_pf;
            Vertex vertice1_pt;
            Vertex vertice2_pt;

            double distancia_1;
            double distancia_2;
            double distancia_3;
            double distancia_4;
            double maior_distancia;
            double menor_distancia;

            Vertex closestp1;
            Vertex closestp2;
            Vertex closestp3;
            Vertex closestp4;


            foreach (OBJECT_ID piso in pisos)
            {
                nome_piso = ValorIfcName(piso);

                if (nome_piso == "PISO 000")
                {
                    api.MessageBox("", "Análise do " + nome_piso, 0);

                    foreach (OBJECT_ID parede in paredes)
                    {
                        string descricao = ValorIfcName(parede);

                        if (descricao == "FACHADA PRINCIPAL")  //da erro aqui: " a referencia de objeto nao foi definida como  uma instancia de um objeto" (????????)
                        {
                            largura_pf = ValorPropriedade2(parede, "Width");

                            int t = ArestasObjeto(parede).Length;

                            Edge[] arestas_parede_frontal = new Edge[t];

                            arestas_parede_frontal = ArestasObjeto(parede);

                            foreach (Edge aresta_pf in arestas_parede_frontal)
                            {
                                vertice1_pf = aresta_pf.v1;
                                vertice2_pf = aresta_pf.v2;

                                foreach (OBJECT_ID p in paredes)
                                {
                                    string id = ValorIfcName(p);

                                    if (id == "FACHADA DE TARDOZ")
                                    {
                                        largura_pt = ValorPropriedade2(p, "Width");

                                        int tamanho = ArestasObjeto(p).Length;

                                        Edge[] arestas_parede_traseira = new Edge[tamanho];

                                        arestas_parede_traseira = ArestasObjeto(p);

                                        foreach (Edge aresta_pt in arestas_parede_traseira)
                                        {
                                            vertice1_pt = aresta_pt.v1;
                                            vertice2_pt = aresta_pt.v2;

                                            //FindDistanceToSegment(vertice, endline1, endline2, out closest)

                                            distancia_1 = FindDistanceToSegment(vertice1_pt, vertice1_pf, vertice2_pf, out closestp1);
                                            todas_distancias.Add(distancia_1);

                                            distancia_2 = FindDistanceToSegment(vertice2_pt, vertice1_pf, vertice2_pf, out closestp2);
                                            todas_distancias.Add(distancia_2);

                                            distancia_3 = FindDistanceToSegment(vertice1_pf, vertice1_pt, vertice2_pt, out closestp3);
                                            todas_distancias.Add(distancia_3);

                                            distancia_4 = FindDistanceToSegment(vertice2_pf, vertice1_pt, vertice2_pt, out closestp4);
                                            todas_distancias.Add(distancia_4);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // maior_distancia = todas_distancias.Max(); - a maior distancia é na diagonal, entao não se usa isto

                    // A maior profundidade (distancia entre as paredes de fachada frontal e traseira) é a menor distancia entre elas + suas espessuras
                    menor_distancia = todas_distancias.Min();

                    profundidade = menor_distancia + largura_pf + largura_pt;

                    if (profundidade <= profundidade_max_RC)
                    {
                        api.MessageBox("Artigo 43º PDM", "A profundidade máxima da construção é respeitada no rés do chão", 0);
                    }

                    //colocar a exceçao da cláusula 2:

                    //foreach (OBJECT_ID espaço in espaços)
                    //{
                    //    nome_espaço = ValorPropriedade2(espaço, "Name");

                    //    if (nome_espaço == "Área de implantação")
                    //    {

                    //    }
                    //}

                }
            }


            // Pisos acima do RC:

            foreach (OBJECT_ID piso in pisos)
            {
                nome_piso = ValorIfcName(piso);

                if (nome_piso.Contains("PISO") && nome_piso != "PISO 000")
                {
                    todas_distancias.Clear(); //limpar a lista de distancias para cada piso
                    
                    api.MessageBox("", "Análise do " + nome_piso, 0);
                    
                    foreach (OBJECT_ID parede in paredes)
                    {
                        string descricao = ValorIfcName(parede);

                        if (descricao == "FACHADA PRINCIPAL")
                        {
                            largura_pf = ValorPropriedade2(parede, "Width");

                            int t = ArestasObjeto(parede).Length;

                            Edge[] arestas_parede_frontal = new Edge[t];

                            arestas_parede_frontal = ArestasObjeto(parede);

                            foreach (Edge aresta_pf in arestas_parede_frontal)
                            {
                                vertice1_pf = aresta_pf.v1;
                                vertice2_pf = aresta_pf.v2;

                                foreach (OBJECT_ID p in paredes)
                                {
                                    string id = ValorIfcName(p);

                                    if (id == "FACHADA DE TARDOZ")
                                    {
                                        largura_pt = ValorPropriedade2(p, "Width");

                                        int tamanho = ArestasObjeto(p).Length;

                                        Edge[] arestas_parede_traseira = new Edge[tamanho];

                                        arestas_parede_traseira = ArestasObjeto(p);

                                        foreach (Edge aresta_pt in arestas_parede_traseira)
                                        {
                                            vertice1_pt = aresta_pt.v1;
                                            vertice2_pt = aresta_pt.v2;

                                            //FindDistanceToSegment(vertice, endline1, endline2, out closest)

                                            distancia_1 = FindDistanceToSegment(vertice1_pt, vertice1_pf, vertice2_pf, out closestp1);
                                            todas_distancias.Add(distancia_1);

                                            distancia_2 = FindDistanceToSegment(vertice2_pt, vertice1_pf, vertice2_pf, out closestp2);
                                            todas_distancias.Add(distancia_2);

                                            distancia_3 = FindDistanceToSegment(vertice1_pf, vertice1_pt, vertice2_pt, out closestp3);
                                            todas_distancias.Add(distancia_3);

                                            distancia_4 = FindDistanceToSegment(vertice2_pf, vertice1_pt, vertice2_pt, out closestp4);
                                            todas_distancias.Add(distancia_4);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    // A maior profundidade (distancia entre as paredes de fachada frontal e traseira) é a menor distancia entre elas + suas espessuras
                    menor_distancia = todas_distancias.Min();

                    profundidade = menor_distancia + largura_pf + largura_pt;

                    //api.MessageBox("iudhiu", profundidade.ToString(), 0);

                    if (profundidade <= profundidade_max_acima_RC)
                    {
                        api.MessageBox("Artigo 43º PDM", "A profundidade máxima da construção é respeitada no piso " + nome_piso.ToString(), 0);
                    }
                    else
                    {
                        api.MessageBox("Artigo 43º PDM", "A profundidade máxima da construção NÃO é respeitada no piso " + nome_piso.ToString(), 0);
                    }
                }
            }

            try
            {
                api.Invalidate();
            }
            catch (BIMVision.DemoModeCallLimitException)
            {
            }
        }
    }
    }



// Criar propriedade 'Número de frentes'??

