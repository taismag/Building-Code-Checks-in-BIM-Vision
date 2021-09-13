 using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Art_67º_RGEU
{
    using BIMVision;
    using System.Configuration;
    using System.Diagnostics.Eventing.Reader;
    using System.Security.Permissions;
    using System.Security.Principal;
    using System.Threading;
    public class Art_67_RGEU : Plugin
    {
        private ApiWrapper api;
        private OBJECT_ID allId;
        private int button1;

        public override void GetPluginInfo(ref PluginInfo info)
        {
            info.name = "Art 67º RGEU";
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
            api.SetButtonText(button1, "RGEU Artigo 67º", "Test");

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


        // PONTO DENTRO DE UM ESPAÇO 2D
        //FONTE: https://stackoverflow.com/questions/43139118/region-isvisiblepointf-has-very-slow-performance-for-large-floating-point-valu
        public bool IsVisible(Vertex p, List<Vertex> points)
        {
            int i, j = points.Count - 1;
            bool isVisible = false;
            for (i = 0; i < points.Count; i++)
            {
                if (points[i].y < p.y && points[j].y >= p.y
                    || points[j].y < p.y && points[i].y >= p.y)
                {
                    if (points[i].x + (p.x - points[i].y) / (points[j].y - points[i].y)
                        * (points[j].x - points[i].x) < p.x)
                    {
                        isVisible = !isVisible;
                    }
                }
                j = i;
            }
            return isVisible;
        }



        // FUNÇÃO PARA FILTRAR QUAIS ESPAÇOS INTERIORES ESTAO DENTRO DE UMA FRAÇAO:
        // precisa testar pro piso terreo porque vao ter mais áreas de exceçoes 
        public List<OBJECT_ID> FiltroEspaços(string nome_fraçao)
        {
            List<OBJECT_ID> pisos = new List<OBJECT_ID>();
            List<OBJECT_ID> espaços = new List<OBJECT_ID>();
            List<OBJECT_ID> espaço_habitaçoes = new List<OBJECT_ID>();
            List<OBJECT_ID> espaço_compartimento_interior = new List<OBJECT_ID>();
            List<Vertex> vertices_fraçao = new List<Vertex>();

            string id_espaço;
            string nome_espaço;
            string nome_piso;
            string nome_piso_fraçao = "";

            Vertex vertice_e1;
            Vertex vertice_e2;
            Vertex vertice_e3;
            Vertex vertice_e4;

            bool pertence_1;
            bool pertence_2;

            //Filtro de objetos IfcSpace
            espaços = FiltrarElementos("IfcSpace");

            // vertices ifcspace da fraçao:
            foreach (OBJECT_ID espaço in espaços)
            {
                id_espaço = ValorIfcName(espaço);

                if (id_espaço == nome_fraçao)
                {
                    nome_piso_fraçao = ValorPropriedade2(espaço, "Storey");

                    int tamanho = ArestasObjeto(espaço).Length;

                    Edge[] arestas_espaço = new Edge[tamanho];

                    arestas_espaço = ArestasObjeto(espaço);

                    foreach (Edge aresta_e in arestas_espaço)
                    {
                        vertice_e1 = aresta_e.v1;
                        vertices_fraçao.Add(vertice_e1);

                        vertice_e2 = aresta_e.v2;
                        vertices_fraçao.Add(vertice_e2);
                    }
                }
            }

            //vertices de todos os outros ifcspaces:
            //(ou filtrar apenas pros ifcspace interiores)

            foreach (OBJECT_ID espaço in espaços)
            {
                nome_espaço = ValorIfcName(espaço);
                nome_piso = ValorPropriedade2(espaço, "Storey"); // confirmar se essa propriedade 'Storey' sempre vai existir 

                if (nome_piso == nome_piso_fraçao && !nome_espaço.Contains("FRAÇÃO") && !nome_espaço.Contains("ABC")) // o código considera que o ponto está dentro, mesmo se ele estiver coincidente (no limite) do contorno da fraçao
                {
                    int t = ArestasObjeto(espaço).Length;

                    Edge[] arestas_espaço_int = new Edge[t];

                    arestas_espaço_int = ArestasObjeto(espaço);

                    foreach (Edge aresta_ei in arestas_espaço_int)
                    {
                        vertice_e3 = aresta_ei.v1;
                        vertice_e4 = aresta_ei.v2;

                        pertence_1 = IsVisible(vertice_e3, vertices_fraçao);
                        pertence_2 = IsVisible(vertice_e4, vertices_fraçao);

                        if (pertence_1 == true && pertence_2 == true)
                        {
                            espaço_compartimento_interior.Add(espaço);

                        }
                    }
                }
            }

            //Todos os comodos daquela fraçao - Retorna a lista de espaços interiores que estao dentro da fraçao:
            return espaço_compartimento_interior.Distinct().ToList();
        }




        // Função para dizer a tipologia da fração:

        public string Tipologia(string nome_fraçao)
        {
            List<OBJECT_ID> quartos = new List<OBJECT_ID>();
            List<OBJECT_ID> comodos_internos = new List<OBJECT_ID>();
            List<OBJECT_ID> quarto_casal = new List<OBJECT_ID>();
            List<OBJECT_ID> quarto_duplo = new List<OBJECT_ID>();
            List<OBJECT_ID> quarto_simples = new List<OBJECT_ID>();

            int num_quartos;
            string tipologia;
            double area_quarto;
            string nome;

            comodos_internos = FiltroEspaços(nome_fraçao);

            foreach (OBJECT_ID comodo in comodos_internos)
            {
                nome = ValorIfcName(comodo);

                if (nome.Contains("QUARTO"))
                {
                    quartos.Add(comodo);
                }
            }

            //api.MessageBox("numero quartos", quartos.Count().ToString(), 0);

            foreach (OBJECT_ID quarto in quartos)
            {
                area_quarto = ValorPropriedade2(quarto, "NetFloorArea");

                if (area_quarto >= 10.5)
                {
                    quarto_casal.Add(quarto);
                    //api.MessageBox("1", area_quarto.ToString(), 0);
                }

                else if (area_quarto >= 9)
                {
                    quarto_duplo.Add(quarto);
                }

                else if (area_quarto >= 6.5)
                {
                    quarto_simples.Add(quarto);
                }
            }

            int num_quartos_casal = quarto_casal.Count();
            int num_quartos_duplos = quarto_duplo.Count();
            int num_quartos_simples = quarto_simples.Count();

            num_quartos = num_quartos_casal + num_quartos_duplos + num_quartos_simples;

            tipologia = "T" + num_quartos.ToString();

            //api.MessageBox("", tipologia, 0);

            return tipologia;
        }



        // função para pegar o IfcName dos elementos (o que aparece em Element Specific no BIM Vision)
        public dynamic ValorIfcName(OBJECT_ID objeto)
        {
            var propriedades = api.GetProperties(objeto, 0); //usa o pset de numero 0 (propriedades que ficam em cima e não pertencem a pset nenhum

            var resultado = "";

            foreach (BIMVision.ApiWrapper.Property p in propriedades)
            {
                string nome = p.name;
                int number = p.nr;

                if (nome == "Name")
                {
                    var valor = p.value.value_str;

                    resultado = valor;
                }
            }

            return resultado;
        }



        //FUNÇÃO PARA DAR A ÁREA DO ESPAÇO

        public double AreaEspaço(string nome_espaço)
        {
            //Filtro de objetos IfcSpace
            List<OBJECT_ID> espaços = new List<OBJECT_ID>();
            espaços = FiltrarElementos("IfcSpace");

            string apartamento;
            double area = 0;

            foreach (OBJECT_ID espaço in espaços)
            {
                apartamento = ValorIfcName(espaço);

                if (apartamento == nome_espaço)
                {
                    area = Math.Round(ValorPropriedade2(espaço, "NetFloorArea"), 2);
                }
            }

            return area;
        }
      



    private void buttonClick()
        {
            List<OBJECT_ID> numero_quartos = new List<OBJECT_ID>();

            
            double area_comp;
            string nome_comp;

            bool aprovado_sala = false;
            bool aprovado_cozinha = false;
            bool aprovado_quarto_casal = false;
            bool aprovado_quarto_duplo = false;
            bool aprovado_quarto_simples = false;

            //quantidade de quartos duplos: cont_qd
            int cont_qd = 0;
            //quantidade de quartos simples: cont_qs
            int cont_qs = 0;

            Array T;

            string nome_espaço;
            double area_bruta_apt;


            //Filtro de objetos IfcSpace
            
            List<OBJECT_ID> espaços = new List<OBJECT_ID>();
            espaços = FiltrarElementos("IfcSpace");

            foreach (OBJECT_ID espaço in espaços)
            {
                nome_espaço = ValorIfcName(espaço);

                if (nome_espaço.Contains("FRAÇÃO"))
                {
                    api.MessageBox("", "ANÁLISE DA " + nome_espaço, 0);

                    List<OBJECT_ID> compartimentos_internos = new List<OBJECT_ID>();

                    compartimentos_internos = FiltroEspaços(nome_espaço);

                    string tipologia = Tipologia(nome_espaço); // ESSA FUNÇAO USA A DE CIMA... FAZ SENTIDO USAR AS DUAS???? (USA A DE CIMA 2X)

                    
                    if (tipologia == "T0")
                    {
                        foreach (OBJECT_ID compartimento in compartimentos_internos)
                        {
                            nome_comp = ValorIfcName(compartimento);

                            if (nome_comp == "SALA")
                            {
                                area_comp = ValorPropriedade2(compartimento, "Area");

                                if (area_comp >= 10)
                                {
                                    aprovado_sala = true;
                                }
                            }

                            else if (nome_comp == "COZINHA")
                            {
                                area_comp = ValorPropriedade2(compartimento, "Area");

                                if (area_comp >= 6)
                                {
                                    aprovado_cozinha = true;
                                }
                            }
                        }

                        if (aprovado_sala == true && aprovado_cozinha == true)
                        {
                            area_bruta_apt = AreaEspaço(nome_espaço);

                            api.MessageBox("Artigo 67º RGEU", "Área bruta mínima T0: 35", 0);
                            api.MessageBox("Artigo 67º RGEU", "Área bruta real do fogo: " + area_bruta_apt.ToString(), 0);

                            if (area_bruta_apt >= 35)
                            {
                                api.MessageBox("Artigo 67º RGEU", "A fração " + nome_espaço + ", de tipologia " + tipologia + ", atende os requisitos mínimos de área", 0);
                            }
                        }
                    }

                    else if (tipologia == "T1")
                    {
                        foreach (OBJECT_ID compartimento in compartimentos_internos)
                        {
                            nome_comp = ValorIfcName(compartimento);

                            if (nome_comp == "SALA")
                            {
                                area_comp = ValorPropriedade2(compartimento, "Area");

                                if (area_comp >= 10)
                                {
                                    aprovado_sala = true;
                                }
                            }

                            else if (nome_comp == "COZINHA")
                            {
                                area_comp = ValorPropriedade2(compartimento, "Area");

                                if (area_comp >= 6)
                                {
                                    aprovado_cozinha = true;
                                }
                            }

                            else if (nome_comp == "QUARTO CASAL")
                            {
                                area_comp = ValorPropriedade2(compartimento, "Area");

                                if (area_comp >= 10.5)
                                {
                                    aprovado_quarto_casal = true;
                                }
                            }

                            
                        }

                        if (aprovado_sala == true && aprovado_cozinha == true && aprovado_quarto_casal == true)
                        {
                            area_bruta_apt = AreaEspaço(nome_espaço);

                            api.MessageBox("Artigo 67º RGEU", "Área bruta mínima T1: 52", 0);
                            api.MessageBox("Artigo 67º RGEU", "Área bruta real do fogo: " + area_bruta_apt.ToString(), 0);

                            if (area_bruta_apt >= 52)
                            {
                                api.MessageBox("Artigo 67º RGEU", "A fração " + nome_espaço + ", de tipologia " + tipologia + ", atende os requisitos mínimos de área", 0);
                            }
                        }

                    }

                    else if (tipologia == "T2")
                    {
                        // api.MessageBox("", "é esse", 0);

                        foreach (OBJECT_ID compartimento in compartimentos_internos)
                        {
                            nome_comp = ValorIfcName(compartimento);
                            //api.MessageBox("", nome_comp, 0);

                            if (nome_comp == "SALA")
                            {
                                //api.MessageBox("", "leu sala", 0);

                                area_comp = ValorPropriedade2(compartimento, "NetFloorArea");

                                if (area_comp >= 12)
                                {
                                    aprovado_sala = true;
                                }
                            }

                            else if (nome_comp == "COZINHA")
                            {
                                //api.MessageBox("", "leu cozinha", 0);

                                area_comp = ValorPropriedade2(compartimento, "NetFloorArea");

                                if (area_comp >= 6)
                                {
                                    aprovado_cozinha = true;
                                }
                            }

                            else if (nome_comp == "QUARTO CASAL")
                            {
                                // api.MessageBox("", "leu quarto casal", 0);

                                area_comp = ValorPropriedade2(compartimento, "NetFloorArea");

                                //api.MessageBox("", area_comp.ToString(), 0); // erro aqui!!!!

                                if (area_comp >= 10.5)
                                {
                                    aprovado_quarto_casal = true;
                                }
                            }

                            else if (nome_comp == "QUARTO DUPLO")
                            {
                                //api.MessageBox("", "leu quarto duplo", 0);

                                area_comp = ValorPropriedade2(compartimento, "NetFloorArea");

                                if (area_comp >= 9)
                                {
                                    aprovado_quarto_duplo = true;
                                }
                            }
                        }

                        if (aprovado_sala == true && aprovado_cozinha == true && aprovado_quarto_casal == true && aprovado_quarto_duplo == true)
                        {
                            area_bruta_apt = AreaEspaço(nome_espaço);

                            api.MessageBox("Artigo 67º RGEU", "Área bruta mínima T2: 72", 0);
                            api.MessageBox("Artigo 67º RGEU", "Área bruta real do fogo: " + area_bruta_apt.ToString(), 0);

                            if (area_bruta_apt >= 72)
                            {
                                api.MessageBox("Artigo 67º RGEU", "A fração " + nome_espaço + ", de tipologia " + tipologia + ", atende os requisitos mínimos de área", 0);
                            }
                        }

                        
                    }

                    else if (tipologia == "T3")
                    {
                        // api.MessageBox("", "é esse", 0);

                        foreach (OBJECT_ID compartimento in compartimentos_internos)
                        {
                            nome_comp = ValorIfcName(compartimento);
                            //api.MessageBox("", nome_comp, 0);

                            if (nome_comp == "SALA")
                            {
                                //api.MessageBox("", "leu sala", 0);

                                area_comp = ValorPropriedade2(compartimento, "NetFloorArea");

                                if (area_comp >= 12)
                                {
                                    aprovado_sala = true;
                                }
                            }

                            else if (nome_comp == "COZINHA")
                            {
                                //api.MessageBox("", "leu cozinha", 0);

                                area_comp = ValorPropriedade2(compartimento, "NetFloorArea");

                                if (area_comp >= 6)
                                {
                                    aprovado_cozinha = true;
                                }
                            }

                            else if (nome_comp == "QUARTO CASAL")
                            {
                                // api.MessageBox("", "leu quarto casal", 0);

                                area_comp = ValorPropriedade2(compartimento, "NetFloorArea");

                                //api.MessageBox("", area_comp.ToString(), 0);

                                if (area_comp >= 10.5)
                                {
                                    aprovado_quarto_casal = true;
                                }
                            }

                            else if (nome_comp == "QUARTO DUPLO")
                            {
                                // api.MessageBox("", "leu quarto duplo", 0);
                                cont_qd += 1;
                                area_comp = ValorPropriedade2(compartimento, "NetFloorArea");

                                if (area_comp >= 9 && cont_qd == 2) //testar isso !!!!!!!!
                                {
                                    aprovado_quarto_duplo = true;
                                }
                            }
                        }

                        if (aprovado_sala == true && aprovado_cozinha == true && aprovado_quarto_casal == true && aprovado_quarto_duplo == true)
                        {
                            area_bruta_apt = AreaEspaço(nome_espaço);

                            api.MessageBox("Artigo 67º RGEU", "Área bruta mínima T3: 91", 0);
                            api.MessageBox("Artigo 67º RGEU", "Área bruta real do fogo: " + area_bruta_apt.ToString(), 0);

                            if (area_bruta_apt >= 91)
                            {
                                api.MessageBox("Artigo 67º RGEU", "A fração " + nome_espaço + ", de tipologia " + tipologia + ", atende os requisitos mínimos de área", 0);
                            }
                        }

                       
                    }

                    else if (tipologia == "T4")
                    {
                        // api.MessageBox("", "é esse", 0);

                        foreach (OBJECT_ID compartimento in compartimentos_internos)
                        {
                            nome_comp = ValorIfcName(compartimento);
                            api.MessageBox("", nome_comp, 0);

                            if (nome_comp == "SALA")
                            {
                                //api.MessageBox("", "leu sala", 0);

                                area_comp = ValorPropriedade2(compartimento, "NetFloorArea");

                                if (area_comp >= 12)
                                {
                                    aprovado_sala = true;
                                }
                            }

                            else if (nome_comp == "COZINHA")
                            {
                                //api.MessageBox("", "leu cozinha", 0);

                                area_comp = ValorPropriedade2(compartimento, "NetFloorArea");

                                if (area_comp >= 6)
                                {
                                    aprovado_cozinha = true;
                                }
                            }

                            else if (nome_comp == "QUARTO CASAL")
                            {
                                // api.MessageBox("", "leu quarto casal", 0);

                                area_comp = ValorPropriedade2(compartimento, "NetFloorArea");

                                api.MessageBox("", area_comp.ToString(), 0);

                                if (area_comp >= 10.5)
                                {
                                    aprovado_quarto_casal = true;
                                }
                            }

                            else if (nome_comp == "QUARTO DUPLO")
                            {
                                // api.MessageBox("", "leu quarto duplo", 0);
                                cont_qd += 1;
                                area_comp = ValorPropriedade2(compartimento, "NetFloorArea");

                                if (area_comp >= 9 && cont_qd == 2) //testar isso !!!!!!!!
                                {
                                    aprovado_quarto_duplo = true;
                                }
                            }

                            else if (nome_comp == "QUARTO SIMPLES")
                            {
                                // api.MessageBox("", "leu quarto simples", 0);

                                area_comp = ValorPropriedade2(compartimento, "NetFloorArea");

                                api.MessageBox("", area_comp.ToString(), 0);

                                if (area_comp >= 6.5)
                                {
                                    aprovado_quarto_simples = true;
                                }
                            }
                        }

                        if (aprovado_sala == true && aprovado_cozinha == true && aprovado_quarto_casal == true && aprovado_quarto_duplo == true && aprovado_quarto_simples == true)
                        {
                            area_bruta_apt = AreaEspaço(nome_espaço);

                            api.MessageBox("Artigo 67º RGEU", "Área bruta mínima T4: 105", 0);
                            api.MessageBox("Artigo 67º RGEU", "Área bruta real do fogo: " + area_bruta_apt.ToString(), 0);

                            if (area_bruta_apt >= 105)
                            {
                                api.MessageBox("Artigo 67º RGEU", "A fração " + nome_espaço + ", de tipologia " + tipologia + ", atende os requisitos mínimos de área", 0);
                            }
                        }
                    }

                    else if (tipologia == "T5")
                    {
                        // api.MessageBox("", "é esse", 0);

                        foreach (OBJECT_ID compartimento in compartimentos_internos)
                        {
                            nome_comp = ValorIfcName(compartimento);
                            api.MessageBox("", nome_comp, 0);

                            if (nome_comp == "SALA")
                            {
                                //api.MessageBox("", "leu sala", 0);

                                area_comp = ValorPropriedade2(compartimento, "NetFloorArea");

                                if (area_comp >= 16)
                                {
                                    aprovado_sala = true;
                                }
                            }

                            else if (nome_comp == "COZINHA")
                            {
                                //api.MessageBox("", "leu cozinha", 0);

                                area_comp = ValorPropriedade2(compartimento, "NetFloorArea");

                                if (area_comp >= 6)
                                {
                                    aprovado_cozinha = true;
                                }
                            }

                            else if (nome_comp == "QUARTO CASAL")
                            {
                                // api.MessageBox("", "leu quarto casal", 0);

                                area_comp = ValorPropriedade2(compartimento, "NetFloorArea");

                                api.MessageBox("", area_comp.ToString(), 0);

                                if (area_comp >= 10.5)
                                {
                                    aprovado_quarto_casal = true;
                                }
                            }

                            else if (nome_comp == "QUARTO DUPLO")
                            {
                                // api.MessageBox("", "leu quarto duplo", 0);
                                cont_qd += 1;
                                area_comp = ValorPropriedade2(compartimento, "NetFloorArea");

                                if (area_comp >= 9 && cont_qd == 3) //testar isso !!!!!!!!
                                {
                                    aprovado_quarto_duplo = true;
                                }
                            }

                            else if (nome_comp == "QUARTO SIMPLES")
                            {
                                // api.MessageBox("", "leu quarto simples", 0);

                                area_comp = ValorPropriedade2(compartimento, "NetFloorArea");

                                api.MessageBox("", area_comp.ToString(), 0);

                                if (area_comp >= 6.5)
                                {
                                    aprovado_quarto_simples = true;
                                }
                            }
                        }

                        if (aprovado_sala == true && aprovado_cozinha == true && aprovado_quarto_casal == true && aprovado_quarto_duplo == true && aprovado_quarto_simples == true)
                        {
                            area_bruta_apt = AreaEspaço(nome_espaço);

                            api.MessageBox("Artigo 67º RGEU", "Área bruta mínima T5: 122", 0);
                            api.MessageBox("Artigo 67º RGEU", "Área bruta real do fogo: " + area_bruta_apt.ToString(), 0);

                            if (area_bruta_apt >= 122)
                            {
                                api.MessageBox("Artigo 67º RGEU", "A fração " + nome_espaço + ", de tipologia " + tipologia + ", atende os requisitos mínimos de área", 0);
                            }
                        }

                        
                    }

                    else if (tipologia == "T6")
                    {
                        // api.MessageBox("", "é esse", 0);

                        foreach (OBJECT_ID compartimento in compartimentos_internos)
                        {
                            nome_comp = ValorIfcName(compartimento);
                            api.MessageBox("", nome_comp, 0);

                            if (nome_comp == "SALA")
                            {
                                //api.MessageBox("", "leu sala", 0);

                                area_comp = ValorPropriedade2(compartimento, "NetFloorArea");

                                if (area_comp >= 16)
                                {
                                    aprovado_sala = true;
                                }
                            }

                            else if (nome_comp == "COZINHA")
                            {
                                //api.MessageBox("", "leu cozinha", 0);

                                area_comp = ValorPropriedade2(compartimento, "NetFloorArea");

                                if (area_comp >= 6)
                                {
                                    aprovado_cozinha = true;
                                }
                            }

                            else if (nome_comp == "QUARTO CASAL")
                            {
                                // api.MessageBox("", "leu quarto casal", 0);

                                area_comp = ValorPropriedade2(compartimento, "NetFloorArea");

                                api.MessageBox("", area_comp.ToString(), 0);

                                if (area_comp >= 10.5)
                                {
                                    aprovado_quarto_casal = true;
                                }
                            }

                            else if (nome_comp == "QUARTO DUPLO")
                            {
                                // api.MessageBox("", "leu quarto duplo", 0);
                                cont_qd += 1;
                                area_comp = ValorPropriedade2(compartimento, "NetFloorArea");

                                if (area_comp >= 9 && cont_qd == 3) //testar isso !!!!!!!!
                                {
                                    aprovado_quarto_duplo = true;
                                }
                            }

                            else if (nome_comp == "QUARTO SIMPLES")
                            {
                                // api.MessageBox("", "leu quarto simples", 0);
                                cont_qs += 1;
                                area_comp = ValorPropriedade2(compartimento, "NetFloorArea");

                                api.MessageBox("", area_comp.ToString(), 0);

                                if (area_comp >= 6.5 && cont_qs == 2)
                                {
                                    aprovado_quarto_simples = true;
                                }
                            }
                        }

                        if (aprovado_sala == true && aprovado_cozinha == true && aprovado_quarto_casal == true && aprovado_quarto_duplo == true && aprovado_quarto_simples == true)
                        {
                            area_bruta_apt = AreaEspaço(nome_espaço);

                            api.MessageBox("Artigo 67º RGEU", "Área bruta mínima T6: 134", 0);
                            api.MessageBox("Artigo 67º RGEU", "Área bruta real do fogo: " + area_bruta_apt.ToString(), 0);

                            if (area_bruta_apt >= 134)
                            {
                                api.MessageBox("Artigo 67º RGEU", "A fração " + nome_espaço + ", de tipologia " + tipologia + ", atende os requisitos mínimos de área", 0);
                            }
                        }

                    }

                    // Outras tipologias:
                    else
                    {

                        T = tipologia.ToCharArray();

                        object r = T.GetValue(1);

                        string letra = Convert.ToString(r);

                        int num = Int32.Parse(letra);
                        //api.MessageBox("numero de quartos TX", num.ToString(), 0);

                        int n_qd = num - 3; // quantidade de quartos duplos
                        //api.MessageBox("numero de quartos duplos", n_qd.ToString(), 0);
                        
                        List<double> area_espaços_habitaveis = new List<double>();

                        foreach (OBJECT_ID compartimento in compartimentos_internos)
                        {
                            nome_comp = ValorIfcName(compartimento);
                            //api.MessageBox("", nome_comp, 0);

                            if (nome_comp == "SALA")
                            {
                                //api.MessageBox("", "leu sala", 0);

                                area_comp = ValorPropriedade2(compartimento, "NetFloorArea");

                                if (area_comp >= 16)
                                {
                                    aprovado_sala = true;

                                    area_espaços_habitaveis.Add(area_comp);
                                }
                            }

                            else if (nome_comp == "COZINHA")
                            {
                                //api.MessageBox("", "leu cozinha", 0);

                                area_comp = ValorPropriedade2(compartimento, "NetFloorArea");

                                if (area_comp >= 6)
                                {
                                    aprovado_cozinha = true;

                                    // area_espaços_habitaveis.Add(area_comp);
                                }
                            }

                            else if (nome_comp == "QUARTO CASAL")
                            {
                                // api.MessageBox("", "leu quarto casal", 0);

                                area_comp = ValorPropriedade2(compartimento, "NetFloorArea");

                                //api.MessageBox("", area_comp.ToString(), 0);

                                if (area_comp >= 10.5)
                                {
                                    aprovado_quarto_casal = true;

                                    area_espaços_habitaveis.Add(area_comp);
                                }
                            }

                            // AUMENTA A QUANTIDADE DE QUARTO DUPLO CONFORME A TIPOLOGIA
                            else if (nome_comp == "QUARTO DUPLO")
                            {
                                cont_qd += 1;
                                area_comp = ValorPropriedade2(compartimento, "NetFloorArea");
                                //api.MessageBox("", "TESTE", 0);

                                if (area_comp >= 9 && cont_qd == n_qd) //testar isso !!!!!!!!
                                {
                                    aprovado_quarto_duplo = true;

                                    area_espaços_habitaveis.Add(area_comp);
                                }
                            }

                            else if (nome_comp == "QUARTO SIMPLES")
                            {
                                // api.MessageBox("", "leu quarto simples", 0);
                                cont_qs += 1;
                                area_comp = ValorPropriedade2(compartimento, "NetFloorArea");

                                //api.MessageBox("", area_comp.ToString(), 0);

                                if (area_comp >= 6.5 && cont_qs == 2)
                                {
                                    aprovado_quarto_simples = true;

                                    area_espaços_habitaveis.Add(area_comp);
                                }
                            }
                        }

                        if (aprovado_sala == true && aprovado_cozinha == true && aprovado_quarto_casal == true && aprovado_quarto_duplo == true && aprovado_quarto_simples == true)
                        {
                            double area_habitavel = area_espaços_habitaveis.Sum();

                            area_bruta_apt = AreaEspaço(nome_espaço);

                            double area_minima_Tx = 1.6 * area_habitavel;

                            //api.MessageBox("Artigo 67º RGEU", "Área bruta mínima T6: 134", 0);
                            api.MessageBox("Artigo 67º RGEU", "Área bruta real do fogo: " + area_bruta_apt.ToString(), 0);

                            if (area_bruta_apt >= area_minima_Tx)
                            {
                                api.MessageBox("Artigo 67º RGEU", "A fração " + nome_espaço + ", de tipologia " + tipologia + ", atende os requisitos mínimos de área", 0);
                            }
                        }  
                    }

                }

            }

























            /*
            List<OBJECT_ID> espaços = new List<OBJECT_ID>();
            List<OBJECT_ID> espaço_habitaçoes = new List<OBJECT_ID>();
            List<OBJECT_ID> espaço_compartimento_interior = new List<OBJECT_ID>();
            List<double> area_habitavel_apt = new List<double>();

            //Filtro de objetos IfcSpace
            espaços = FiltrarElementos("IfcSpace");

            bool habitavel;

            string nome_espaço;
            string description_espaço;
            string tipologia;
            string longname_espaço;
            string nome_apt_tx; // tipologia T x>6

            double area_bruta_apt;
            double area_bruta_tx;
            double area_habitavel;
            double soma_area_habitavel;
            double area_bruta_min_tx;

            //Para cada IfcSpace, pegar as propriedades que quero...
            foreach (OBJECT_ID espaço in espaços)
            {
                description_espaço = ValorPropriedade2(espaço, "Description");
                longname_espaço = ValorPropriedade2(espaço, "LongName");

                if (description_espaço == "Habitação")
                {
                    espaço_habitaçoes.Add(espaço);
                }

                else if (description_espaço == "Compartimento interior")
                {
                    espaço_compartimento_interior.Add(espaço);
                }
            }

            foreach (OBJECT_ID espaço_habitaçao in espaço_habitaçoes)
            {
                nome_espaço = ValorPropriedade2(espaço_habitaçao, "Name");
                area_bruta_apt = ValorPropriedade2(espaço_habitaçao, "Area");
                tipologia = ValorPropriedade2(espaço_habitaçao, "Tipologia");

                if (tipologia == "T0")
                {
                    api.MessageBox("Artigo 67º RGEU", "Área bruta mínima T0: 35", 0);
                    api.MessageBox("Artigo 67º RGEU", "Área bruta real do fogo: " + area_bruta_apt.ToString(), 0);

                    if (area_bruta_apt >= 35)
                    {
                        api.MessageBox("Artigo 67º RGEU", "O Fogo " + nome_espaço + " atende os requisitos mínimos de área", 0);
                    }
                }

                else if (tipologia == "T1")
                {
                    api.MessageBox("Artigo 67º RGEU", "Área bruta mínima T1: 52", 0);
                    api.MessageBox("Artigo 67º RGEU", "Área bruta real do fogo: " + area_bruta_apt.ToString(), 0);

                    if (area_bruta_apt >= 52)
                    {
                        api.MessageBox("Artigo 67º RGEU", "O Fogo " + nome_espaço + " atende os requisitos mínimos de área", 0);
                    }
                }

                else if (tipologia == "T2")
                {
                    api.MessageBox("Artigo 67º RGEU", "Área bruta mínima T2: 72", 0);
                    api.MessageBox("Artigo 67º RGEU", "Área bruta real do fogo: " + area_bruta_apt.ToString(), 0);

                    if (area_bruta_apt >= 72)
                    {
                        api.MessageBox("Artigo 67º RGEU", "O Fogo " + nome_espaço + " atende os requisitos mínimos de área", 0);
                    }
                }

                else if (tipologia == "T3")
                {
                    api.MessageBox("Artigo 67º RGEU", "Área bruta mínima T3: 91", 0);
                    api.MessageBox("Artigo 67º RGEU", "Área bruta real do fogo: " + area_bruta_apt.ToString(), 0);

                    if (area_bruta_apt >= 91)
                    {
                        api.MessageBox("Artigo 67º RGEU", "O Fogo " + nome_espaço + " atende os requisitos mínimos de área", 0);
                    }
                }

                else if (tipologia == "T4")
                {
                    api.MessageBox("Artigo 67º RGEU", "Área bruta mínima T4: 105", 0);
                    api.MessageBox("Artigo 67º RGEU", "Área bruta real do fogo: " + area_bruta_apt.ToString(), 0);

                    if (area_bruta_apt >= 105)
                    {
                        api.MessageBox("Artigo 67º RGEU", "O Fogo " + nome_espaço + " atende os requisitos mínimos de área", 0);
                    }
                }

                else if (tipologia == "T5")
                {
                    api.MessageBox("Artigo 67º RGEU", "Área bruta mínima T5: 122", 0);
                    api.MessageBox("Artigo 67º RGEU", "Área bruta real do fogo: " + area_bruta_apt.ToString(), 0);

                    if (area_bruta_apt >= 122)
                    {
                        api.MessageBox("Artigo 67º RGEU", "O Fogo " + nome_espaço + " atende os requisitos mínimos de área", 0);
                    }
                }

                else if (tipologia == "T6")
                {
                    api.MessageBox("Artigo 67º RGEU", "Área bruta mínima T6: 134", 0);
                    api.MessageBox("Artigo 67º RGEU", "Área bruta real do fogo: " + area_bruta_apt.ToString(), 0);

                    if (area_bruta_apt >= 134)
                    {
                        api.MessageBox("Artigo 67º RGEU", "O Fogo " + nome_espaço + " atende os requisitos mínimos de área", 0);
                    }
                }

                else
                {
                    area_bruta_tx = area_bruta_apt;
                    nome_apt_tx = nome_espaço;

                    //area habitável:
                    foreach (OBJECT_ID espaço_interior in espaço_compartimento_interior)
                    {
                        description_espaço = ValorPropriedade2(espaço_interior, "Description");
                        longname_espaço = ValorPropriedade2(espaço_interior, "LongName");

                        if (longname_espaço == nome_apt_tx) //aqui esta o problema
                        {
                            habitavel = ValorPropriedade2(espaço_interior, "Habitável");
                            area_habitavel = ValorPropriedade2(espaço_interior, "Area");

                            api.MessageBox("aapt", nome_apt_tx, 0); //pra testar - Não funciona

                            if (habitavel == true) 
                            {
                                area_habitavel_apt.Add(area_habitavel);

                                api.MessageBox("area habit", area_habitavel_apt.ToString(), 0); //pra testar
                            }
                        }
                    }

                    soma_area_habitavel = Math.Round(area_habitavel_apt.Sum(), 2);
                    area_bruta_min_tx = 1.6 * soma_area_habitavel;

                    api.MessageBox("Artigo 67º RGEU", "Área bruta mínima " + tipologia + ": " + area_bruta_min_tx.ToString(), 0);
                    api.MessageBox("Artigo 67º RGEU", "Área bruta real do fogo: " + area_bruta_tx.ToString(), 0);

                    if (area_bruta_tx >= area_bruta_min_tx)
                    {
                        api.MessageBox("Artigo 67º RGEU", "O Fogo " + nome_espaço + " atende os requisitos mínimos de área", 0);
                    }
                }
            }*/


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
