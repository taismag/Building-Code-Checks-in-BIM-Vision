using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Artigo_41_PDM
{
    using BIMVision;
    using System.Configuration;
    using System.Diagnostics.Eventing.Reader;
    using System.Security.Permissions;
    using System.Security.Principal;
    using System.Threading;

    public class Art41PDM : Plugin
    {
        private ApiWrapper api;

        private OBJECT_ID allId;
        private int button1;

        public override void GetPluginInfo(ref PluginInfo info)
        {
            info.name = "Artº 41 PDM";
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
            api.SetButtonText(button1, "PDM Artigo 41º", "Test");

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


        //public Edge[] ArestasObjeto(OBJECT_ID objeto)   //PRECISA CONSERTAR ISSO... O TAMANHO NAO PODE SER 12
        //{

        //    //List<Edge> arestas = new List<Edge>();

        //    var geometria = api.FirstGeometry(objeto);

        //    if (geometria == true)
        //    {
        //        api.GetGeometry();

        //        var arestas = api.GetGeometryEdges();

        //        //api.MessageBox("numero arestas", arestas.Count().ToString(), 0);

        //        int tamanho = arestas.Length;

        //        Edge[] res = new Edge[tamanho];

        //        foreach (Edge aresta in arestas)
        //        {
        //            res.Append(aresta);
        //        }

        //        //var res = new Edge[tamanho];

        //        return res;
        //    }

        //    else
        //    {
        //        return null;
        //    }
        //}

        public Edge[] ArestasObjeto(OBJECT_ID objeto)
        {
            Edge[] arestas = new Edge[0];
            //api.MessageBox("funçao arestas 1", arestas.Length.ToString(), 0);

            //List<Edge> arestas = new List<Edge>();

            var geometria = api.FirstGeometry(objeto);

            if (geometria == true)
            {
                api.GetGeometry();
                arestas = api.GetGeometryEdges();
                int tamanho = arestas.Length;
                //api.MessageBox("funçao arestas 2", tamanho.ToString(), 0);
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
            List<Vertex> vertices_rua_frontal = new List<Vertex>();
            List<Vertex> vertices_parede_frontal = new List<Vertex>();

            List<double> todas_distancias = new List<double>();

            string nome_rua;
            string nome_edificio;
            string nome_nivel;

            double largura_rua_frontal = 7.0;// tem que colocar atraves de propriedade

            double distancia_alinhamento_eixorua;

            double altura_max_permitida_metros;

            double distancia_1;
            double distancia_2;
            double distancia_3;
            double distancia_4;
            double menor_distancia;

            double elevacao;
            double elevacao_max;
            double elevacao_base;
            double altura_edificio;

            int numero_pisos;
            int altura_max_permitida_pisos;

            Vertex vertice1_rua;
            Vertex vertice2_rua;

            Vertex vertice1_parede;
            Vertex vertice2_parede;

            Vertex closestp1;
            Vertex closestp2;
            Vertex closestp3;
            Vertex closestp4;

            //Filtro de objetos Rua
            List<OBJECT_ID> ruas = new List<OBJECT_ID>();
            ruas = FiltrarElementos("IfcSlab");

            // Filtro paredes
            List<OBJECT_ID> paredes = new List<OBJECT_ID>();
            paredes = FiltrarElementos("IfcWall");

            //Filtro niveis
            List<OBJECT_ID> niveis = new List<OBJECT_ID>();
            niveis = FiltrarElementos("IfcBuildingStorey");

            //Filtro edificios
            List<OBJECT_ID> edificios = new List<OBJECT_ID>();
            edificios = FiltrarElementos("IfcBuilding");


            foreach (OBJECT_ID rua in ruas)
            {
                nome_rua = ValorIfcName(rua);

                if (nome_rua == "ARRUAMENTO FRONTAL")
                {
                    int t = ArestasObjeto(rua).Length;

                    Edge[] arestas_rua_frontal = new Edge[t];

                    arestas_rua_frontal = ArestasObjeto(rua);

                    //api.MessageBox("# aresta da rua", t.ToString(), 0);

                    foreach (Edge aresta_r in arestas_rua_frontal)
                    {
                        vertice1_rua = aresta_r.v1;
                        //api.MessageBox("vertice rua 1", vertice1_rua.x.ToString(), 0);    //deu zero
                        vertice2_rua = aresta_r.v2;
                        //api.MessageBox("vertice rua 2", vertice1_rua.x.ToString(), 0);    //deu zero

                        foreach (OBJECT_ID parede in paredes)
                        {
                            string id = ValorIfcName(parede);
                            if (id == "FACHADA PRINCIPAL")
                            {
                                int tamanho = ArestasObjeto(parede).Length;
                                //api.MessageBox("# aresta da parede", tamanho.ToString(), 0);

                                Edge[] arestas_parede_frontal = new Edge[tamanho];

                                arestas_parede_frontal = ArestasObjeto(parede);

                                foreach (Edge aresta_p in arestas_parede_frontal)
                                {
                                    vertice1_parede = aresta_p.v1;
                                    //api.MessageBox("vertice parede 1", vertice1_parede.x.ToString(), 0);    //deu zero
                                    vertice2_parede = aresta_p.v2;
                                    //api.MessageBox("vertice parede 2", vertice2_parede.y.ToString(), 0);    //deu zero

                                    //FindDistanceToSegment(vertice, endline1, endline2, out closest)

                                    distancia_1 = FindDistanceToSegment(vertice1_rua, vertice1_parede, vertice2_parede, out closestp1);
                                    todas_distancias.Add(distancia_1);

                                    distancia_2 = FindDistanceToSegment(vertice2_rua, vertice1_parede, vertice2_parede, out closestp2);
                                    todas_distancias.Add(distancia_2);

                                    distancia_3 = FindDistanceToSegment(vertice1_parede, vertice1_rua, vertice2_rua, out closestp3);
                                    todas_distancias.Add(distancia_3);

                                    distancia_4 = FindDistanceToSegment(vertice2_parede, vertice1_rua, vertice2_rua, out closestp4);
                                    todas_distancias.Add(distancia_4);
                                }
                            }
                        }
                    }
                }
            }

            //api.MessageBox(".", todas_distancias.Count().ToString(), 0);

            //todas as distancias deram zero ???????????

            //var eu = todas_distancias.Max();
            //api.MessageBox("eu", eu.ToString(), 0);

            //var oi = todas_distancias.Min();
            //api.MessageBox("oi", oi.ToString(), 0);

            menor_distancia = todas_distancias.Min(); // o problema ta aqui

            //api.MessageBox("md", menor_distancia.ToString(), 0); //deu zero

            distancia_alinhamento_eixorua = Math.Round((menor_distancia + (largura_rua_frontal / 2)), 3);

            api.MessageBox("daer", distancia_alinhamento_eixorua.ToString(), 0);

            altura_max_permitida_metros = 2 * distancia_alinhamento_eixorua;

            api.MessageBox(".", "parte 2 ok", 0);


            //Altura máxima permitida - pisos:
            elevacao_max = elevacao_base = 0;
            string ed_principal;

            foreach (OBJECT_ID nivel in niveis)         //Precisa testar com niveis de base diferentes:
            {
                nome_nivel = ValorPropriedade2(nivel, "Identifier");

                if (nome_nivel == "EDIFÍCIO PRINCIPAL")
                {
                    ed_principal = ValorIfcName(nivel);
                    if (ed_principal.Contains("BASE TERRENO NATURAL"))
                    {
                        elevacao_base = ValorPropriedade2(nivel, "Elevation");
                    }

                    ed_principal = ValorIfcName(nivel);
                    if (ed_principal == "CÉRCEA MÁXIMA EP")
                    {
                        elevacao_max = ValorPropriedade2(nivel, "Elevation");
                    }
                }
            }

            // verificar se funciona:
            api.MessageBox(".", "oi", 0);
            numero_pisos = 0;
            foreach (OBJECT_ID nivel in niveis)
            {
                string identificador = ValorPropriedade2(nivel, "Identifier");

                if (identificador == "EDIFÍCIO PRINCIPAL")
                {
                    nome_nivel = ValorIfcName(nivel);

                    if (nome_nivel.Contains("PISO"))
                    {
                        numero_pisos += 1;
                    }
                }
            }

            altura_edificio = Math.Round(elevacao_max - elevacao_base, 3);  //deu 14.8

            altura_max_permitida_pisos = 6;

            // clausula 5: admite-se 2 pisos em qualquer situaçao. para fazer esta, precisa-se determinar a clausula 6

            // pelo que entendi (cláusula 6), a camara deve fazer a conversao de altura em numero de pisos... se for isso, mudar o codigo


            if (numero_pisos <= altura_max_permitida_pisos && altura_edificio <= altura_max_permitida_metros)
            {
                api.MessageBox("Artº 41 PDM", numero_pisos.ToString() + " é menor ou igual a " + altura_max_permitida_pisos.ToString(), 0);
                api.MessageBox("Artº 41 PDM", altura_edificio.ToString() + " é menor do que " + altura_max_permitida_metros.ToString(), 0);
                api.MessageBox("Artº 41 PDM", "A altura respeita o artigo 41º do PDM", 0);
            }

            else
            {
                api.MessageBox("Artº 41 PDM", numero_pisos.ToString() + " deveria ser menor ou igual a " + altura_max_permitida_pisos.ToString(), 0);
                api.MessageBox("Artº 41 PDM", altura_edificio.ToString() + " deveria ser menor do que " + altura_max_permitida_metros.ToString(), 0);
                api.MessageBox("Artº 41 PDM", "A altura NÃO respeita o artigo 41º do PDM", 0);
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

//ARTIGO DE APLICAÇÃO GERAL:
//PDM Artigo 41º
//PUAR Artigo 22º
//RGEU Artigo 59º


/*  Regras de modelação para este artigo:
 *  
 *  ******************************************************************     
 *  IfcRoof:
 *  
 *      IfcName:
 *          Telhado principal: Telhado principal
 *          Telhado dos anexos: Telhado anexo
 *  
 *  ******************************************************************    
 *  IfcWall:
 *  
 *      IfcExportAs: IfcWall (neste projeto exportou como IfcWallStandardCase, mas não pode) - consertar
 *      IfcName: Fachada lateral D, Fachada lateral E, Fachada de tardoz, Fachada principal
 *      
 *  OBS: quando a parede é desenhada com as extremidades acabando e começando no lufgar errado, da um erro (pequeno)
 
 *  
 *  ******************************************************************    
 *  IfcBuildingElementProxy:
 *  
 *      IfcName:
 *          Rua frontal: Rua frontal
 *          Ruas laterias: Rua lateral E, Rua lateral D
 *          
 *  OBS: ainda preciso achar uma maneira de pegar a largura da rua... coloquei manualmente porque não há uma propriedade que dê esse valor
 *      
 *      
 *  ******************************************************************   
 *  IfcBuildingStorey:
 *  
 *      IfcName:
 *          Base:
 *          Pisos:
 *          Cobertura:
 *      
 *      Elevation
 *  
 *  
 *  ******************************************************************    
 *  IfcBuilding:
 * 
 *      IfcName:
 *          Principal: Edifício principal
 *          Edifícios adjacentes: Edifício adjacente 1D (lado direito), Edifício adjacente 1E (lado esquerdo), Edifício adjacente T (traseiro)
 *      
 *      NumberOfStoreys: preencher para o edifício principal
 *      
 *  OBS: pelo que vi, acho que não é possível separar o edifício principal no Revit. No BIM Vision aparece o edificio principal como sendo tudo 
 *  que tem no projeto (incluindo os edificios adjacentes) e os edificios adjacentes aparecem também individualmente como IfcBuilding (que 
 *  atribuí pela propriedade IfcExportAs no Revit antes de exportar).
 *  
 *  
 *  ******************************************************************************************************************************************************************************************************
 *  
 *  Possíveis erros (testar):
 *      ifcwall versus ifcwallstandardcase
 *      terreno inclinado
 *      posição negativa - modelar com coordenadas positivas
 *      definir o nivel para medir a altura (se for piso 0, medir dai. senao, fazer outro nivel - ver se ele exporta)
 *  
 */