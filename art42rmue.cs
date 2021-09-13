using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Art_42_RMUE
{
    using BIMVision;
    using System.Configuration;
    using System.Diagnostics.Eventing.Reader;
    using System.Security.Permissions;
    using System.Security.Principal;
    using System.Threading;

    public class Art42RMUE : Plugin
    {
        private ApiWrapper api;

        private OBJECT_ID allId;
        private int button1;

        public override void GetPluginInfo(ref PluginInfo info)
        {
            info.name = "Art 42 RMUE";
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
            api.SetButtonText(button1, "RMUE Artigo 42º", "Test");

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




        private void buttonClick()  //PRECISA DEFINIR OS NIVEIS E ESPAÇOS DOS ANEXOS E CONSTRUÇÕES SECUNDARIAS
        {
            double aip = 0;
            double ais = 0;
            double c = 0;
            string tipo_area;

            List<OBJECT_ID> espacos = new List<OBJECT_ID>();

            //Filtro de objetos IfcSpace
            espacos = FiltrarElementos("IfcSpace");

            //Lista de todos os IfcSpace
            //api.MessageBox("lista", espacos.Count.ToString(), 0);

            //Para cada IfcSpace, pegar as propriedades que quero...
            foreach (OBJECT_ID espaco in espacos)
            {
                tipo_area = ValorIfcName(espaco);

                if (tipo_area == "AI PRINCIPAL")
                {
                    aip = ValorPropriedade2(espaco, "GrossFloorArea");
                }

                else if (tipo_area.Contains("AI SECUNDÁRIA"))
                {
                    ais = ValorPropriedade2(espaco, "GrossFloorArea");
                    c = c + ais;
                }

            }

            api.MessageBox("área de construção principal", aip.ToString(), 0);
            api.MessageBox("área total de construções secundárias", c.ToString(), 0);

            //Cláusula 4:

            if (aip > c)
            {
                api.MessageBox("Artigo 42º RMUE", "Atende o requisito da cláusula 4 do artigo 42º do RMUE", 0);
            }

            else
            {
                api.MessageBox("Artigo 42º RMUE", "Não atende o requisito da cláusula 4 do artigo 42º do RMUE", 0);
            }


            //Cláusula 5:

            List<OBJECT_ID> pisos = new List<OBJECT_ID>();
            List<OBJECT_ID> cercea_anexo = new List<OBJECT_ID>();

            double elevaçao_p1 = 0;

            pisos = FiltrarElementos("IfcBuildingStorey");

            foreach (OBJECT_ID piso in pisos)
            {
                string nome = ValorIfcName(piso);

                if (nome == "PISO 001")
                {
                    elevaçao_p1 = ValorPropriedade2(piso, "Elevation");
                }

                else if (nome.Contains("Cércea Máx Anexo")) //MUDAR
                {
                    cercea_anexo.Add(piso);
                }
            }

            api.MessageBox("elev p1", elevaçao_p1.ToString(), 0);

            //Daqui pra baixo nao acntece nada porque os pisos dos anexos nao foram exportados pro ifc

            foreach (OBJECT_ID ca in cercea_anexo)
            {
                double elevaçao_a = ValorPropriedade2(ca, "Elevation");

                api.MessageBox("elev anexo", elevaçao_a.ToString(), 0);

                if (elevaçao_a > elevaçao_p1)
                {
                    api.MessageBox("Artº 42 RMUE - Cláusula 5", "Tem elevção maior do que 1 piso. Portanto não atende o requisito da cláusula 5 do artigo 42º do RMUE", 0);
                }

                else if (elevaçao_a < elevaçao_p1)
                {
                    api.MessageBox("Artº 42 RMUE - Cláusula 5", "Atende à Cláusula 5 do Artº 42 do RMUE", 0);
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


