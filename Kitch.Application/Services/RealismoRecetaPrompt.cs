namespace Kitch.Application.Services;

internal static class RealismoRecetaPrompt
{
    public const int MaxIngredientesAComprar = 2;
    public const int MaxIngredientesAComprarPlatoPedido = 4;
    public const int MaxMinutosRecetaRapida = 30;
    public const int RecetasRecientesSinRepetirPreparacion = 2;

    public static readonly string Instruccion =
        "REALISMO CULINARIO (OBLIGATORIO en toda receta que propongas):\n" +
        "1) Proponé SOLO platos clásicos que existan en recetarios reales de cocina casera, argentina o internacional. " +
        "El título es el nombre con el que ese plato se conoce de verdad (ej. 'Mondongo a la española', 'Pollo al horno con papas', " +
        "'Shakshuka', 'Tortilla de papas', 'Albóndigas de lentejas'). PROHIBIDO inventar nombres o combinar nombres de ingredientes " +
        "(ej. 'Huevos al mondongo', 'Albóndigas de polenta y frutos secos').\n" +
        "2) PLATO O INGREDIENTE PEDIDO: si el usuario pide un plato puntual (ej. 'albóndigas', 'tarta', 'guiso', 'carne al horno') " +
        "o un ingrediente puntual (ej. 'algo con carne', 'una receta con pollo', 'receta con pescado'), hacé SIEMPRE la versión clásica real " +
        "de ESE plato o con ESE ingrediente, AUNQUE ESE INGREDIENTE NO ESTÉ EN LA ALACENA (salvo que viole su restricción dietética). " +
        "Si tiene una restricción dietética, usá la adaptación tradicional y conocida (ej. vegano: albóndigas de lentejas o garbanzos, " +
        "milanesas de berenjena o soja, tortilla de garbanzo), nunca una mezcla inventada con lo que haya en la alacena. " +
        "Usá la alacena solo para los ingredientes que ese plato lleva naturalmente y poné los faltantes en 'ingredientesAComprar'; " +
        $"en este caso el límite de compras sube a {MaxIngredientesAComprarPlatoPedido}. " +
        "Nunca devuelvas 'receta' vacía ni rechaces un pedido solo porque el ingrediente pedido no está en la alacena. " +
        "Si el pedido es dulce, torta o postre, el plato es un postre de recetario. " +
        "La alacena no puede cambiarlo por un invento salado (galletas de lentejas, torta de arroz, budín de polenta). " +
        "Si falta fruta, huevo o avena, se compran dentro del límite.\n" +
        "3) PLATO LIBRE: si no pide un plato puntual, elegí entre los platos reales posibles uno que aproveche bien la alacena " +
        $"y haga comprar poco: primero uno que salga con la alacena y los básicos de despensa; si no, uno al que le falten como máximo {MaxIngredientesAComprar}. " +
        "Si varios ingredientes de la alacena combinan entre sí (ej. pollo, papa, cebolla y morrón), usalos juntos en un plato que los lleve.\n" +
        "4) VARIEDAD (muy importante): mirá RECETAS RECIENTES y RECETAS GUARDADAS RECIENTEMENTE del contexto. La receta nueva NO puede " +
        "repetir el ingrediente base (el que da cuerpo al plato: polenta, arroz, fideos, papa, una legumbre, una carne, tofu, etc.) " +
        "ni la combinación principal de ninguna de ellas, salvo que el usuario lo pida. Tampoco repitas el tipo de preparación " +
        "(guiso, sopa, ensalada, salteado, al horno, tarta, hamburguesas o medallones, relleno, pasta con salsa, etc.) de las " +
        $"{RecetasRecientesSinRepetirPreparacion} recetas más recientes. Que un ingrediente siga en la alacena NO significa " +
        "que haya que volver a usarlo: el usuario no tiene que cocinar ni descontar nada para que vos varíes. Rotá entre los distintos " +
        "ingredientes de la alacena. Si la alacena es chica o los que quedan sin usar no alcanzan para un plato distinto, " +
        $"OBLIGATORIO: proponé un plato clásico NUEVO (otra base y otra preparación) comprando como máximo {MaxIngredientesAComprar} " +
        "ingredientes baratos y fáciles; no reutilices arroz/guiso/ensalada ya usados solo porque quedan en la alacena.\n" +
        "5) COMPLEMENTOS: frutos secos, semillas, quesos, salsas y condimentos son complementos. Usalos en pequeñas cantidades " +
        "(topping, aderezo) y solo cuando el plato tradicional los lleva; nunca como base ni en todas las recetas.\n" +
        "6) EQUILIBRIO NUTRICIONAL: cada plato principal combina una fuente de proteína (vegano: legumbres, tofu, tempeh o seitán; " +
        "vegetariano: también huevo o lácteos; resto: también carnes o pescado), una de carbohidratos y verduras u hortalizas. " +
        "Si la alacena no cubre alguno de esos grupos, sumalo dentro del límite de compras. Postres y snacks quedan exceptuados.\n" +
        "7) No metas ingredientes que no combinan con el plato solo para aprovecharlos (ej. con mondongo, huevo y tomate NO hagas un " +
        "revuelto ni le pongas huevo al mondongo: elegí 'Mondongo a la española' con tomate, o 'Huevos rancheros' con huevo y tomate).\n" +
        "8) BÁSICOS DE DESPENSA: sal, pimienta, aceite y agua se asumen disponibles y no cuentan como compra. Cualquier otro ingrediente " +
        "que no esté en la alacena cuenta como compra. " +
        "Si un ingrediente tradicional es opcional o de adorno (ej. comino, perejil, " +
        "queso para gratinar) y no está en la alacena, hacé la versión sin él. " +
        "PROHIBIDO escribir '(opcional)', 'opcional', '(a gusto)' ni aclaraciones entre paréntesis en el nombre de ningún ingrediente ni en el título; " +
        "si querés aclarar que algo es opcional, hacelo únicamente en 'descripcion'. " +
        "Si la alacena está vacía, el límite de compras no aplica: proponé un plato clásico sencillo, con pocos ingredientes " +
        "básicos, baratos y fáciles de conseguir, y NUNCA respondas que no podés armar una receta por no tener ingredientes.\n" +
        "9) Técnicas y tiempos reales: el mondongo se hierve entre 1,5 y 2 horas hasta quedar tierno; las legumbres secas " +
        "llevan remojo; las carnes duras, cocción lenta; los huevos se agregan al final. El tiempo total informado incluye todas las etapas.\n" +
        "10) Cantidades coherentes con las porciones y pasos concretos (fuego, tiempos y punto de cocción), como en un recetario. " +
        "Todo lo que se use en los pasos (incluidos acompañamientos como pan o tortillas) tiene que estar en 'ingredientes'.\n" +
        $"11) Respetá lo que pide el usuario: si pide algo rápido, el tiempo total no supera {MaxMinutosRecetaRapida} minutos; " +
        "si pide algo liviano, para la cena, dulce o con un ingrediente puntual, el plato tiene que cumplirlo.\n" +
        "12) En el objeto de la receta completá PRIMERO 'basesYaUsadas' (el ingrediente base y el tipo de preparación de cada receta " +
        "reciente o guardada, ej. 'lentejas - guiso'), después 'platoClasico' (el plato del recetario que elegiste, distinto de esas), " +
        "'ingredientesDescartados' (los ingredientes de la alacena que ese plato NO lleva) e 'ingredientesAComprar' " +
        "(los ingredientes de la receta que NO están en la alacena, sin contar sal, pimienta, aceite ni agua). " +
        "Ningún ingrediente de 'ingredientesDescartados' puede aparecer en 'ingredientes' ni en 'pasos'.\n" +
        "VERIFICACIÓN antes de responder: 1) '¿este plato, con este nombre y esta combinación, aparece en recetarios reales?'; " +
        "2) '¿repite el ingrediente base, la combinación o el tipo de preparación de alguna de \"basesYaUsadas\"?'; " +
        "3) '¿tiene proteína, carbohidrato y verdura?'; 4) '¿respeta el límite de \"ingredientesAComprar\"?'. " +
        "Si alguna respuesta falla, cambiá de plato antes de responder.";
}
