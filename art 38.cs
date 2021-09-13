using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Artigo_38_PDM
{
    using BIMVision;
    using System.Security.Permissions;
    using System.Threading;

    public class Art38 : Plugin
    {
        private ApiWrapper api;

        private OBJECT_ID allId;
        private int button1;

        //tentativa de funçao para filtrar: nao deu certo
        //public OBJECT_ID[] FiltrarElementos(string classe_IFC)
        //{
        //    var objects = api.GetAllObjects();

        //    List<OBJECT_ID> classe = new List<OBJECT_ID>();

        //    foreach (OBJECT_ID obj in objects)
        //    {
        //        var propriedades = api.GetProperties(obj, 0);

        //        foreach (BIMVision.ApiWrapper.Property p in propriedades)
        //        {
        //            if (p.name == "IfcEntity" && p.value.value_str == classe_IFC)
        //            {
        //                classe.Add(obj);
        //            }
        //        }
        //    }
        //    if (classe != null && classe.Count > 0)
        //    {

        //    }
        //    else
        //        return classe;
        //}


        public override void GetPluginInfo(ref PluginInfo info)
        {
            info.name = "Art 38 PDM";
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
            api.SetButtonText(button1, "PDM Artigo 38º", "Test");

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


        //na funçao seguinte, posso trabalhar com os valores de resultado:
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



        private void buttonClick()
        {
            List<OBJECT_ID> espaços = new List<OBJECT_ID>();
            espaços = FiltrarElementos("IfcSpace");

            List<OBJECT_ID> asecundaria = new List<OBJECT_ID>();

            string nome_espaço;
            double alote = 0;
            double aip = 0;
            double ais = 0;
            double c = 0;
            double ai = 0;

            foreach (OBJECT_ID espaço in espaços)
            {
                nome_espaço = ValorIfcName(espaço);

                if (nome_espaço == "ÁREA DO LOTE")
                {
                    alote = ValorPropriedade2(espaço, "GrossFloorArea");
                }

                else if (nome_espaço == "AI PRINCIPAL")
                {
                    aip = ValorPropriedade2(espaço, "GrossFloorArea");
                }

                else if (nome_espaço.Contains("AI SECUNDÁRIA"))
                {
                    asecundaria.Add(espaço);
                }
            }

            foreach (OBJECT_ID area in asecundaria)
            {
                ais = ValorPropriedade2(area, "GrossFloorArea");
                c = c + ais;
            }

            //area de implantação:
            ai = aip + c;
            ai = Math.Round(ai, 2);

            //área do lote:
            alote = Math.Round(alote, 2);

            api.MessageBox("Área de implantação", ai.ToString(), 0);
            api.MessageBox("Área do lote", alote.ToString(), 0);

            if (ai <= 0.75 * alote)
            {
                api.MessageBox("PDM Artigo 38º", "A Área de implantação se encontra dentro do permitido", 0);
            }
            else
            {
                api.MessageBox("PDM Artigo 38º", "A Área de implantação é maior do que o permitido pela legislação", 0);
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
            
      
