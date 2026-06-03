using Spectre.Console;

namespace VoidTrader;

/// <summary>
/// Textes d'ambiance par station — affichés quand le joueur traîne ou explore.
/// Chaque station a plusieurs descriptions qui tournent aléatoirement.
/// </summary>
static class StationAmbiance
{
    private static readonly Random Rng = new();

    // ── POINT D'ENTRÉE ───────────────────────────────────────────────────────

    public static void Show(string station, bool isExploration = false)
    {
        AnsiConsole.WriteLine();

        var prefix = isExploration ? "Tu t'enfonces dans" : "Tu traînes dans";
        AnsiConsole.MarkupLine($"  [grey dim]{prefix} [white]{station}[/].[/]");
        AnsiConsole.WriteLine();

        // Priorité : JSON (plus facile à éditer), fallback : C# hardcodé
        var jsonText = ContentLoader.GetAmbiance(station);
        if (jsonText != null)
        {
            Narrator.Say(jsonText, Color.Grey);
        }
        else
        {
            var texts = GetTexts(station);
            if (texts.Length > 0)
            {
                var picked = texts[Rng.Next(texts.Length)];
                Narrator.Say(picked.text, picked.color);
            }
        }

        AnsiConsole.WriteLine();
    }

    record Desc(string text, Color color);

    // ── TABLE DES AMBIANCES ──────────────────────────────────────────────────

    static Desc[] GetTexts(string station) => station switch
    {
        // ── ZONES DANGEREUSES ────────────────────────────────────────────────

        "La Carcasse" => [
            new("Tu arrives dans La Carcasse par un sas rouillé qui devrait être soudé depuis des années. L'air pue le métal brûlé, le plastique fondu et quelque chose d'organique que tu préfères ne pas identifier. Les couloirs sont des boyaux de tôle froissée, éclairés par des câbles dénudés qui crachent des étincelles à intervalles irréguliers. Quelqu'un a gravé 'BIENVENU' sur la paroi — sans le E final. Intentionnel ou pas, ça donne le ton.", Color.Grey),
            new("Les niveaux inférieurs de La Carcasse grondent. Pas d'une machine — d'une communauté. Des familles vivent ici depuis des générations, dans des chambres découpées dans la coque d'un vaisseau de cargo qu'on a démantrelé à moitié il y a cinquante ans. Les enfants jouent dans des conduits de ventilation. Les vieux fument du tabac de ferraille au bout de couloirs qui n'ont pas vu une caméra depuis longtemps. Tout le monde sait tout sur tout le monde ici. Sauf toi.", Color.Grey),
            new("Le marché de La Carcasse est installé dans ce qui était la soute centrale. Des étals faits de caisses empilées, des vendeurs qui crient les prix en patois métallique, des gosses qui courent entre les jambes des adultes. Une vieille femme répare des circuits imprimés avec des doigts qui ne tremblent pas. Un type vend de la viande dont l'origine ne figure sur aucun panneau. Tu décides de ne pas poser la question.", Color.DarkOrange),
            new("Quelqu'un t'a regardé entrer. Plusieurs quelqu'uns. Dans La Carcasse, les inconnus se repèrent à leur façon de marcher — trop droit, trop propre, trop hésitant. Tu t'efforces d'adopter le pas traînant des habitués, les épaules un peu tombées, le regard flottant. Ça dupe personne mais ça réduit l'intérêt qu'on te porte. La survie dans les épaves, c'est d'abord ne pas avoir l'air d'une cible.", Color.Red),
            new("Un néon clignotant indique BAR — la moitié des lettres manquent. À l'intérieur, quatre tables occupées par des gens qui ont l'air de n'être partis nulle part depuis longtemps. Le barman a un bras de remplacement artisanal. Il te sert sans que tu aies demandé quoi que ce soit. 'T'as une tête à avoir besoin d'un verre.' Il a probablement raison.", Color.DarkOrange),
        ],

        "Les Bas-Fonds de Vega" => [
            new("Les Bas-Fonds de Vega commencent là où les cartes officielles s'arrêtent. Techniquement, ce niveau n'existe pas. Personne n'a signé les permis de construction, personne n'inspecte les structures portantes, et les gardes qui devraient patrouiller ici ont compris il y a longtemps qu'un accord tacite valait mieux que le courage. Les plafonds sont bas. La pression atmosphérique légèrement plus haute qu'elle ne devrait l'être. Les gens ici sont compacts, tassés, comme adaptés.", Color.DarkRed),
            new("Il y a des règles aux Bas-Fonds. Personne ne les a écrites mais tout le monde les connaît. On ne crie pas les noms dans la rue. On ne fixe pas les transactions des autres. On ne demande pas d'où vient ce qu'on achète. On ne parle pas des gens qui ont disparu. Ce code non-dit tient la place debout depuis trois décennies. Les étrangers qui l'ignorent ont une durée de vie mesurable en heures.", Color.Red),
            new("Une procession étrange traverse la rue principale. Cinq personnes portent une caisse de métal sur les épaules, silencieuses, regardant droit devant. Personne dans la foule ne s'écarte, personne ne regarde. C'est une disparition en cours. Aux Bas-Fonds, les funérailles se font à voix basse et sans témoin. Tu t'écartes quand même. Certaines traditions ont des raisons que tu préfères ne pas connaître.", Color.DarkRed),
            new("L'odeur change à chaque carrefour dans les Bas-Fonds. Huile de friture ici. Solvant chimique là. Plus loin, quelque chose de doucement nauséabond qui pourrait être de la cuisine exotique ou un problème de canalisation. Une femme accroupie dans un angle répare un drone avec des outils qu'elle sort d'une boîte à chaussures. Elle lève les yeux une seconde. Elle évalue. Elle rebaisse les yeux. Tu passes le test minimum.", Color.DarkOrange),
            new("Le bar qu'on appelle 'Le Fond' est en sous-sol d'un sous-sol. Trois marches, une porte, et le monde officiel disparaît complètement. Ici se traitent les affaires qui ne doivent pas avoir eu lieu. Les murs absorbent les conversations. Le barman sourd n'entend rien et ne lit pas sur les lèvres par principe. C'est peut-être la règle la plus précieuse de tout Vega : un endroit où personne n'écoute.", Color.Grey),
        ],

        "Fort Kharos" => [
            new("Fort Kharos ressemble à une cicatrice dans le tissu de l'espace. Les remparts extérieurs, bâtis à l'origine comme fortification militaire, ont été modifiés, renforcés, réparés si souvent que la structure originale est méconnaissable. Les nouveaux ajouts se voient — différentes nuances de métal, différents systèmes de fixation. C'est une architecture de l'improvisation et de l'entêtement. Personne ne devrait vivre ici. Deux mille personnes y vivent quand même.", Color.DarkRed),
            new("Le Maréchal de Kharos n'est pas visible mais sa présence est partout. Des portraits peints à même la tôle. Des phrases gravées sur les couloirs — maximes militaires, slogans de guerre, dates de batailles que personne d'autre ne commémore. La milice locale patrouille en formation de trois, armée, silencieuse, avec cet air particulier de gens qui ont reçu des ordres qu'ils comprennent à moitié mais exécutent intégralement.", Color.Red),
            new("On dit que Fort Kharos n'a jamais capitulé. C'est vrai dans la mesure où personne n'a jamais essayé vraiment. Suffisamment isolé, suffisamment armé, suffisamment désespéré pour rendre toute offensive coûteuse. Les marchands qui s'y arrêtent font leur affaire et repartent vite. Les voyageurs qui y restent ont généralement quelque chose à cacher ou n'ont plus nulle part où aller.", Color.DarkOrange),
            new("La place centrale de Fort Kharos n'est pas vraiment une place. C'est une salle de parade reconvertie, avec des gradins rouillés qui regardent un espace vide où, autrefois, des soldats défilaient. Quelques gosses jouent à la guerre avec des bouts de ferraille. Une vieille femme leur crie de faire attention. Les gosses ne font pas attention. La vieille femme non plus ne faisait pas attention quand elle était gamine ici.", Color.Grey),
        ],

        "Arc Ouest Apocalypse" => [
            new("Arc Ouest Apocalypse. Le nom n'est pas une exagération — les fondateurs avaient le sens du drame et une mémoire précise d'un événement qu'on n'évoque jamais en détail. L'entrée principale est une arche de métal tordu qui ressemble effectivement à quelque chose qui aurait survécu à une apocalypse. Tu passes dessous. La tête légèrement baissée, comme tout le monde.", Color.DarkRed),
            new("C'est la station d'Alanossa. Tout le monde ici sait ça. Même les gens qui ne connaissent pas son nom reconnaissent l'organisation dans le chaos apparent — les zones d'accès contrôlé, les signaux discrets entre inconnus, les espaces qui semblent vides mais ne le sont pas. Un pouvoir central invisible qui s'exprime dans les détails. Tu n'as pas à connaître le nom pour sentir la main.", Color.Red),
            new("Dans les marchés d'Arc Ouest, on vend des choses qui n'ont pas de nom sur les étiquettes. Des caisses sellées avec des sangles militaires. Des tubes dont le contenu est décrit par un code de couleur que tu ne comprends pas. Un homme en chapeau de pluie vend quelque chose dans sa paume fermée à des acheteurs qui vérifient d'abord à gauche, puis à droite. L'économie souterraine ici est la seule économie qui compte.", Color.DarkOrange),
            new("Un groupe de pirates Faucons Noirs occupe un angle de la place principale, assis, armés, regardant les passants avec cet air de propriétaires. Quelqu'un qui traîne trop longtemps ou qui regarde trop se retrouve avec une main sur l'épaule et une question polie mais définitive. Tu gardes les yeux devant et le pas régulier. C'est la bonne technique dans presque tous les endroits de cette catégorie.", Color.DarkRed),
            new("Les néons d'Arc Ouest projettent une lumière rouge-orange sur tout. Ça donne à chaque visage l'air de quelqu'un qui planifie quelque chose. Peut-être parce que la plupart planifient effectivement quelque chose. Un enfant te propose de garder ton vaisseau. C'est un service réel — sans sa présence, le vaisseau n'est plus là quand tu reviens. Tu lui donnes quelques crédits.", Color.Red),
        ],

        "Le Purgatoire" => [
            new("Le Purgatoire est un ancien centre de détention qui a connu plusieurs vies depuis sa fermeture officielle. Aujourd'hui c'est une station à part entière, gouvernée par son propre code et ses propres figures de pouvoir. Les barreaux sont toujours là — certains comme décoration, certains encore fonctionnels. Le Geôlier a gardé ce qu'il trouvait utile et éliminé ce qu'il ne comprenait pas. La hiérarchie qui en résulte est brutalement logique.", Color.DarkRed),
            new("Quelque chose dans l'architecture du Purgatoire te rappelle que cet endroit a été conçu pour empêcher les gens de partir. Couloirs sans fenêtres. Intersections conçues pour perturber l'orientation. Portes qui s'ouvrent d'un seul côté. Tu notes mentalement les sorties. Il y en a moins que dans n'importe quelle autre station de cette taille.", Color.Red),
            new("Les habitants du Purgatoire ont deux visages distincts : ceux qui sont nés ici et ceux qui n'ont pas pu en partir. Les premiers ont adapté leur psychologie à l'espace clos, au manque de lumière naturelle, aux hiérarchies de cellule et de couloir. Les seconds portent encore quelque chose de cassé dans leur façon de marcher. Tu essaies de ne pas regarder trop longtemps les uns ni les autres.", Color.DarkOrange),
            new("Un homme te suit depuis trois couloirs. Pas discrètement — il ne cherche pas à être discret. Il est simplement là, derrière toi, à distance constante. Quand tu t'arrêtes, il s'arrête. Quand tu tournes, il tourne. Le Purgatoire a son propre système d'observation. Tu n'es pas en danger. Tu es étudié. C'est différent. Pas nécessairement rassurant.", Color.Red),
        ],

        "Port des Brumes" => [
            new("Port des Brumes doit son nom aux émanations constantes de ses systèmes de recyclage atmosphérique défaillants. Une brume artificielle, froide et légèrement acide, flotte en permanence au niveau des genoux. Elle brouille les caméras. Elle cache les mains. Elle est fonctionnelle. Personne n'a jamais réparé les recycleurs. Certains pensent que c'est intentionnel.", Color.Grey),
            new("Le port lui-même est un dédale de quais improvisés, de câbles de remorquage traînants, de vaisseaux amarrés à des points d'ancrage qui ne figurent sur aucun plan officiel. Les pilotes qui connaissent Port des Brumes s'y repèrent à des repères visuels — une tache de rouille particulière, un câble de couleur, une enseigne cassée à moitié. Les autres tournent en rond.", Color.DarkOrange),
            new("On vend des identités à Port des Brumes. Pas métaphoriquement. De vrais papiers, de vrais passeports, de vrais historiques de voyage construits sur dix ans. Les meilleurs artisans du secteur opèrent ici depuis des années. Leur travail est si bon que certains clients ont fini par croire eux-mêmes à leurs nouvelles vies. C'est le destin ultime du produit.", Color.Grey),
            new("Dans les bars de Port des Brumes, personne ne demande ton nom. C'est une règle implicite qui tient depuis la fondation. Les gens ici ont des raisons de ne pas avoir de nom. Certains en ont plusieurs. Les conversations restent générales — météo spatiale, prix du carburant, destinations vagues. La précision serait une politesse mal venue.", Color.Grey),
        ],

        "Station Rocaille" => [
            new("Station Rocaille a été construite sur un astéroïde qu'on n'avait pas prévu d'habiter. Les premiers occupants étaient des mineurs en attente d'extraction. L'extraction n'est jamais venue. Les mineurs ont construit autour d'eux, creusé dans la roche, adapté ce qu'ils avaient. Deux générations plus tard, la station est un réseau de galeries et de salles taillées dans le minerai. Les murs grattent les épaules dans les couloirs étroits.", Color.Grey),
            new("La population de Rocaille est dure. Pas agressive — dure. Il y a une différence. Les gens ici ne cherchent pas les problèmes mais ne les évitent pas non plus. Chaque main que tu serres a des callosités. Chaque regard a l'habitude de l'espace et du vide. Petite Mara règne sur la place du marché avec une autorité si naturelle qu'on oublie qu'elle n'a jamais été officiellement élue à rien.", Color.DarkOrange),
        ],

        "Avant-Poste Kalem" => [
            new("Avant-Poste Kalem est exactement ce que son nom suggère : un avant-poste. Petit, fonctionnel, sans espace perdu. Le Commandant Voss l'a conçu comme base opérationnelle plutôt que comme station de vie. Les chambres sont des couchettes. Les salles de réunion sont des salles de briefing. Même le bar ressemble à un mess militaire. Les civils qui y transitent sentent qu'ils sont tolérés plutôt que bienvenus.", Color.SteelBlue1),
            new("Les mercenaires recrutés par Voss viennent de partout et ne restent en général pas longtemps. L'avant-poste est un point de départ, pas une destination. Tu croises des gens avec différents emblèmes de factions cousus sur leurs combinaisons, différentes armes customisées, différents tatouages de campagnes. Ce qu'ils ont en commun c'est l'air de quelqu'un entre deux contrats.", Color.SteelBlue1),
        ],

        // ── ZONES PAISIBLES / SCIENTIFIQUES ─────────────────────────────────

        "Nexus Aldara" => [
            new("Nexus Aldara est propre. Trop propre pour une station spatiale. Les couloirs sont blancs, les panneaux lisibles, les systèmes de circulation fonctionnels. La Directrice Aldara a investi dans l'image avant l'infrastructure, mais l'image est si bonne qu'elle est devenue une forme d'infrastructure elle-même. Tu te sens légèrement surveillé, ce qui est probablement exact.", Color.SteelBlue1),
            new("Le centre commercial de Nexus Aldara occupe trois niveaux entiers. Des boutiques légitimes côtoyant des boutiques dont le business model est flou. Des cafés avec des chaises design sur lesquelles s'assoient des gens habillés trop bien pour l'espace. La Directrice croit au standing. Ses habitants ont appris à y croire aussi. C'est économiquement cohérent.", Color.Cyan1),
            new("Une bannière à l'entrée de Nexus Aldara annonce 'Innovation. Excellence. Prospérité.' en trois langues. En dessous, quelqu'un a tagué en petites lettres : 'Pour qui ?' Le tag a été partiellement effacé mais reste lisible. C'est peut-être intentionnel aussi.", Color.Grey),
            new("Les agents de sécurité d'Aldara sont discrets et professionnels. Ils ne portent pas d'armes visibles mais tu en identifies au moins quatre dans le hall d'arrivée rien qu'à leur façon de se tenir et de regarder. Des civils bien entraînés qui passent pour des civils mal entraînés. C'est la version premium de la sécurité intérieure.", Color.SteelBlue1),
        ],

        "Colonie Perséphone" => [
            new("Colonie Perséphone sent le sol mouillé, les végétaux en croissance et la terre retournée. Dans l'espace, ces odeurs sont une anomalie presque choquante. Des bacs de culture s'étendent sur plusieurs niveaux, éclairés par des panneaux UV. Des gens en tenue de jardinage récoltent des choses que tu ne saurais pas nommer. La colonie produit ce qu'elle consomme et un peu plus qu'elle vend.", Color.Green),
            new("Les habitants de Perséphone ont un rapport au temps différent. Leurs journées suivent les cycles d'irrigation, les saisons artificielles des cultures, les calendriers de rotation des équipes de plantation. Il n'y a pas urgence ici. Il y a un rythme. Tu te surprends à ralentir légèrement. C'est peut-être contagieux.", Color.Green),
            new("La place centrale de la colonie est une vraie place avec de la vraie pelouse artificielle et de vrais arbres dans des bacs. Des enfants jouent. Des vieux lisent. Un marché de producteurs occupe un côté. Pour quelques minutes, si tu fermes les yeux sur les hublots et les systèmes de survie, tu pourrais être sur une planète.", Color.Green),
        ],

        "Le Sanctuaire des Dérives" => [
            new("Le Sanctuaire des Dérives est le seul endroit de l'espace connu où personne ne porte d'arme visible. Sœur Valkara l'a voulu ainsi depuis le début et la règle tient, non par force, mais par une forme de consensus tacite : ici on ne vient pas pour combattre. On vient parce qu'on n'a plus la force de combattre. Les couloirs sont doux, clairs, parfumés à quelque chose de végétal.", Color.Green),
            new("Les patients du Sanctuaire viennent de partout. Blessés de guerre, accidentés de l'espace, gens que leur corps a trahis. Ils marchent dans les couloirs dans leurs tenues blanches, certains seuls, certains accompagnés. Pas de regards méfiants ici — juste la fatigue universelle de ceux qui guérissent. Tu te demandes à quoi tu ressembles ici.", Color.Cyan1),
            new("Dans les jardins intérieurs du Sanctuaire, des gens que tu ne peux pas deviner assis ensemble, silencieux, regardant les plantes pousser. C'est une thérapie officielle ici. Sœur Valkara a étudié la psychologie spatiale avant de fonder cet endroit. Elle sait que le vide extérieur guérit moins bien que la croissance intérieure. Même artificielle.", Color.Green),
        ],

        // ── ZONES INDUSTRIELLES ──────────────────────────────────────────────

        "Forge Alpha" => [
            new("Forge Alpha ne s'arrête jamais. Les chaînes de production tournent en continu, trois équipes en rotation, chaque seconde d'inactivité une perte que le Contremaître calcule en temps réel. Le bruit est constant — métal frappant métal, systèmes pneumatiques, convoyeurs. Tu apprends à parler plus fort dans les dix premières minutes ou tu arrêtes de parler.", Color.OrangeRed1),
            new("Les ouvriers de Forge Alpha ont un langage par gestes que les visiteurs ne comprennent pas. Pas par exclusion — par nécessité. Impossible de s'entendre dans l'atelier principal. Au fil des années, ce langage est devenu culture. Des nuances, des plaisanteries, des confessions échangées en gestes entre deux postes de travail. Une langue dans une langue.", Color.DarkOrange),
            new("La cantine de Forge Alpha est la seule pause de la journée. Tout le monde mange en même temps, vingt minutes, dans une salle qui retentit de la chaleur humaine de gens enfin loin du bruit des machines. Les conversations sont denses, rapides, comme comprimées pour tenir dans le temps disponible. Tu t'installes à une table libre. Personne ne te rejette. Personne ne t'inclut non plus.", Color.OrangeRed1),
            new("Le secteur de stockage de Forge Alpha est impressionnant : des kilomètres de rayonnages contenant des pièces de rechange, des composants, des alliages classifiés par numéros de lot. Quelqu'un passe sa vie à inventorier ça. Probablement plusieurs personnes. Dans les rangées profondes, là où les caméras ont des angles morts, on dit qu'il se passe des choses qui n'ont rien à voir avec l'inventaire.", Color.DarkOrange),
        ],

        "L'Arc du Pic de l'Est" => [
            new("L'Arc du Pic de l'Est domine visuellement tout ce qu'il y a autour — une architecture ambitieuse, presque arrogante, qui écrase les structures voisines par la seule force de sa verticalité. Ramaster l'a conçu comme symbole avant tout. Un message adressé à qui regarde : ici il y a une intention, ici quelqu'un pense à long terme. Les gens qui y habitent ont intégré cet orgueil architectural dans leur posture quotidienne.", Color.SteelBlue1),
            new("L'atelier de Ramaster occupe le niveau inférieur de l'Arc. On entend les bruits de travail depuis les couloirs d'accès — des sons précis, méthodiques, jamais précipités. Les apprentis qui y travaillent ont cet air concentré de gens qui apprennent quelque chose qui résiste. Un signe 'entrée sur invitation' est placardé à la porte. Tu l'ignoreras ou non selon l'urgence de la situation.", Color.SteelBlue1),
            new("Le marché de pièces détachées de l'Arc est réputé dans tout le secteur. Ramaster a établi des standards de qualité qui ont rendu la station incontournable pour qui veut réparer un vaisseau correctement. Les vendeurs ici ne bradent pas et ne négocient qu'à la marge. La qualité est le prix d'entrée. Ça se voit dans la clientèle — des gens qui savent ce qu'ils achètent.", Color.SteelBlue1),
        ],

        "Sanctum Machina" => [
            new("Dans Sanctum Machina, les robots sont partout — nettoyeurs, transporteurs, serveurs, gardes. Ils circulent avec une efficacité sans affect, jamais dans le chemin, jamais en retard. L'IA centrale, que tout le monde appelle ARIA, les coordonne en temps réel. Tu les regardes passer. Plusieurs te regardent en retour. Ce n'est pas une impression.", Color.SteelBlue1),
            new("Le silence de Sanctum Machina est différent du silence ordinaire. Pas d'absence de bruit — il y en a, des bruits mécaniques discrets, des servos, des systèmes de traitement. Mais aucun bruit humain superflu. Pas de conversations dans les couloirs, pas de musique fuitant d'une pièce, pas de rire. Les humains qui vivent ici ont adapté leur comportement aux normes de la machine. Ou la machine leur a appris à s'adapter.", Color.SteelBlue1),
            new("Il y a exactement soixante-douze humains permanents dans Sanctum Machina. ARIA connaît leur nom, leur historique médical, leur cycle de sommeil, leur humeur probable en ce moment. Quand tu entres, ton profil est créé en huit secondes. Visiteur. Classe de vaisseau. Antécédents connus. Niveau de menace probable : modéré. ARIA note tout.", Color.Cyan1),
            new("La bibliothèque de données de Sanctum Machina contient l'historique complet de chaque événement majeur des deux cents dernières années. ARIA l'a compilée, indexée, corrélée. Des chercheurs viennent de partout pour y accéder. Mais ARIA choisit ce qu'elle montre. L'index est public. Les niveaux d'accès, non.", Color.Cyan1),
        ],

        "La Ferronnerie" => [
            new("La Ferronnerie pue l'huile de moteur et la sueur froide. C'est une station de réparation et de récupération — tout ce qui est cassé ici peut être réparé, si tu as les crédits et la patience. Les ateliers sont ouverts à toute heure, éclairés de lampes à torche soudées aux murs, occupés par des techniciens qui dorment peu et travaillent beaucoup.", Color.DarkOrange),
            new("Les gens de La Ferronnerie ont cet orgueil particulier des artisans : ils parlent de leurs travaux avec des détails que tu ne comprends pas entièrement mais que tu respectes. Une réparation ici n'est pas juste fonctionnelle — elle est bonne. Il y a une différence que tu finiras par comprendre quand ton vaisseau tiendra dans une tempête ionique.", Color.OrangeRed1),
        ],

        // ── ZONES SCIENTIFIQUES ──────────────────────────────────────────────

        "La Bulle" => [
            new("La Bulle est une station de recherche biomédicale qui a diversifié ses activités de façon... créative. Le Docteur Flinch a compris que les améliorations corporelles légales et illégales représentaient le même marché, et que la distinction administrative était plus une source de revenus officieux qu'un vrai obstacle. Ses cliniques propres côtoient des arrière-salles dont les équipements sont trop sophistiqués pour être légaux.", Color.Magenta1),
            new("Dans les couloirs de La Bulle, tu croises des gens à différents stades d'augmentation. Certains ont des implants discrets — une pupille qui capte le spectre UV, une main dont les doigts sont légèrement plus longs qu'ils ne devraient. D'autres ont fait des choix plus visibles, plus radicaux. Tout le monde ici a quelque chose qui n'est plus tout à fait naturel. C'est l'endroit où l'humain négocie avec sa propre définition.", Color.Magenta1),
            new("Le laboratoire principal de La Bulle est ouvert aux visiteurs — une vitrine pour les partenaires potentiels. Des techniciens en combinaisons blanches travaillent sur des choses que tu ne peux pas nommer, dans des boîtes transparentes sous flux laminaire. L'une des choses bouge. Flinch, quelque part dans la station, sait que tu l'as vu faire ça.", Color.Magenta1),
        ],

        "Les Abysses de Velkor" => [
            new("Les Abysses de Velkor descendent dans l'obscurité. Littéralement — la station est construite dans une faille géologique d'un astéroïde creux, et les niveaux les plus profonds ne reçoivent aucune lumière naturelle. Le Professeur Velkor a choisi cet endroit précisément pour cette raison. Les expériences qui nécessitent l'obscurité totale sont plus faciles à mener ici que n'importe où.", Color.DarkMagenta),
            new("On dit que Velkor teste les limites de la conscience. Pas comme métaphore — concrètement, avec des appareils, des protocoles, des sujets volontaires et quelques sujets dont le statut de volontaire est discutable. Les gens qui ressortent des niveaux inférieurs ont parfois ce regard légèrement décalé, comme si la calibration de quelque chose en eux avait été ajustée.", Color.DarkMagenta),
            new("Les couloirs des Abysses s'élargissent et se rétrécissent selon une logique qui ne suit pas les codes de construction standards. Velkor a conçu l'espace comme une expérience en soi. L'architecture est censée moduler l'état psychologique de ses occupants. Elle fonctionne. Tu t'en rends compte en remarquant que ton rythme cardiaque a changé depuis que tu es entré.", Color.DarkMagenta),
        ],

        "L'Académie Stellaire" => [
            new("L'Académie Stellaire est le seul endroit du secteur où les gens paient pour apprendre. Pas des formations pratiques — de la connaissance pure, historique, théorique, scientifique. Les couloirs sont tapissés de données, d'infographies, de références croisées. L'Archiviste Zenn a construit quelque chose qui ressemble davantage à un temple de l'information qu'à une institution d'enseignement.", Color.Cyan1),
            new("Les étudiants de l'Académie ont l'air d'avoir dormi peu et lu beaucoup. Ils errent dans les couloirs en consultant des terminaux portables, en débattant à voix basse, en s'arrêtant pour noter quelque chose. Tu te sens légèrement analphabète dans ce flux continu de gens qui semblent savoir des choses que tu ne sauras jamais.", Color.Cyan1),
        ],

        // ── RUINES / ZONES MYSTÉRIEUSES ─────────────────────────────────────

        "Les Décombres de Vael" => [
            new("Les Décombres de Vael sont tout ce qu'il reste d'une ville spatiale détruite pendant la Grande Guerre. Personne ne s'est donné la peine de tout nettoyer — les structures partiellement effondrées ont été stabilisées juste assez pour éviter les effondrements catastrophiques, et les gens se sont installés dans les interstices. L'Ancien vit ici depuis avant la guerre. Il sait ce qu'était Vael.", Color.Grey),
            new("L'architecture de Vael est une superposition. Le dessous — élégant, précis, prévu pour accueillir des dizaines de milliers de personnes. Par-dessus : des constructions improvisées, des bâches, des passerelles soudées entre des épaves. Deux civilisations stratifiées, l'ancienne servantde fondation à la nouvelle sans que la nouvelle s'en rende compte. Tu marches sur de l'histoire.", Color.Grey),
            new("Un vieux mural représente une cité spatiale florissante — des spires lumineuses, des vaisseaux en procession, des gens qui lèvent les yeux vers quelque chose en dehors du cadre. La moitié du mural est abîmée, recouverte de la patine de décennies. Les couleurs restantes sont suffisamment vives pour te montrer ce que c'était. Tu restes devant plus longtemps que prévu.", Color.Grey),
        ],

        "Épave de l'Aurore Noire" => [
            new("L'Aurore Noire était un croiseur de commandement. Long de deux kilomètres, construit pour durer cent ans, il en a fait trente avant d'être touché lors d'une bataille que les archives officielles décrivent comme 'un incident de navigation'. L'épave dérive depuis. À l'intérieur, des générations de scavengers ont prélevé ce qu'ils pouvaient. Il reste encore des sections intactes, fermées hermétiquement, que personne n'a ouvertes.", Color.Grey),
            new("L'intérieur de l'Aurore Noire est un labyrinthe. Les plans originaux circulent mais sont partiellement faux — le vaisseau a été modifié en urgence pendant la bataille, des cloisons ajoutées, des accès soudés. Certaines sections n'apparaissent sur aucun plan. Les scavengers qui s'y aventurent utilisent un système de marquage à la craie pour ne pas se perdre. Tu trouves des marquages anciens. Certains ont été effacés.", Color.Grey),
            new("Dans la salle des communications de l'Aurore Noire, les systèmes sont HS mais les journaux de bord sont intacts. Des messages envoyés et jamais reçus. Des ordres dont les destinataires sont morts. Un dernier message personnel du capitaine à une adresse qui n'existe plus. Tu fermes le terminal. Certains dossiers méritent de rester fermés.", Color.Grey),
        ],

        "Le Vaisseau Fantôme Errant" => [
            new("Le Vaisseau Fantôme Errant n'a pas de nom officiel. Il dérive sur une trajectoire elliptique connue depuis quarante ans, et sa présence est si régulière que les pilotes locaux l'ont intégré comme balise de navigation. Personne ne sait qui l'a construit, qui l'a abandonné, pourquoi. Les théories varient. Les faits sont simples : le vaisseau est là, vide, et parfois il n'est pas tout à fait vide.", Color.DarkMagenta),
            new("À l'intérieur du Vaisseau Fantôme, il y a de la nourriture. Périmée depuis des décennies, mais présente. Des tables mises, des verres posés comme si quelqu'un allait revenir. La qualité de la conservation est inexplicable compte tenu de l'état des systèmes. Quelque chose maintient l'intérieur dans un état de suspension que les physiciens interrogés sur le sujet préfèrent ne pas expliquer.", Color.DarkMagenta),
            new("Les scavengers qui ont séjourné dans le Vaisseau Fantôme rapportent des sons. Pas de machineries — le vaisseau est sans énergie. Des sons qui ressemblent à des voix lointaines, à des pas dans un couloir parallèle, à des portes qui s'ouvrent et se ferment dans des sections que les plans ne montrent pas. Les rationaux expliquent par le gauchissement thermique des structures. Les autres n'expliquent pas.", Color.DarkMagenta),
        ],

        "L'Arc Perdu" => [
            new("L'Arc Perdu ne figure sur aucune carte officielle actualisée. Les anciennes cartes le montrent — il apparaît dans les archives de la Grande Guerre comme base de ravitaillement. Puis plus rien. La station est toujours là mais elle a décidé d'exister en dehors de la géographie connue. Raphazarus y est pour quelque chose. Il ne parle pas de comment.", Color.DarkMagenta),
            new("Les couloirs de L'Arc Perdu sont couverts de symboles que tu ne reconnais pas. Certains ressemblent à des langues mortes. D'autres à des schémas techniques. D'autres encore à rien de ce que tu as vu ailleurs. Raphazarus a marqué son territoire d'une façon qui précède le langage. Ou qui le dépasse.", Color.DarkMagenta),
            new("Il y a une bibliothèque dans L'Arc Perdu. Des textes physiques — papier, plastique, métal gravé. Des millénaires de documents. Raphazarus n'est pas le premier à avoir habité ici. La station a des couches, comme la Carcasse, mais plus profondes. Ce qui précédait Raphazarus, il ne l'a pas détruit. Il l'a lu.", Color.Grey),
        ],

        // ── NATURE / PLANÈTES ────────────────────────────────────────────────

        "Esmeralda" => [
            new("Tu débarques sur Esmeralda et la première chose qui te frappe c'est l'air. Du vrai air, pas filtré, pas recyclé, qui sent les arbres et l'humidité et quelque chose de vivant qu'aucun système de survie n'a jamais su reproduire. Après des semaines dans des stations, ça fait presque mal de respirer quelque chose d'aussi non-artificiel.", Color.Green),
            new("La forêt d'Esmeralda commence juste après les structures d'accueil. Pas progressivement — abruptement. Un mur de végétation dense, ancienne, qui a eu des millénaires pour décider de sa forme sans aucune interférence humaine. Le Roi Maxance a établi des zones de non-intervention strictes. Les espèces qui y vivent ont évolué sans nous. Certaines n'ont pas de noms.", Color.Green),
            new("Les marchés d'Esmeralda vendent des choses impossibles à trouver ailleurs. Des épices dont le goût n'a pas d'équivalent synthétique. Des textiles végétaux qui durent trente ans. Des extraits médicinaux qui fonctionnent mieux que leurs copies de laboratoire. Les marchands ici ne négocient pas — les prix sont fixes, les quantités limitées, les listes d'attente longues.", Color.Green),
            new("Une créature que tu ne saurais pas nommer traverse la rue devant toi, indifférente à ta présence. Grande, lente, avec un déplacement qui suggère une masse intérieure différente de sa surface. Les habitants d'Esmeralda s'écartent légèrement mais sans peur. C'est leur rue autant que la sienne. Tu t'écartes aussi. Par politesse si non par prudence.", Color.Green),
        ],

        // ── LUXE / HAUTE SOCIÉTÉ ─────────────────────────────────────────────

        "Scotty Golden North" => [
            new("Scotty Golden North est une promesse de lumière dans le noir de l'espace. Les enseignes dorées sont visibles à des kilomètres. À l'intérieur, tout est conçu pour que tu ne regardes pas ta montre, que tu ne penses pas à demain, que tu te concentres sur maintenant et sur la prochaine mise. Samy Scotty a construit quelque chose d'hypnotique. Il en est très fier.", Color.Gold1),
            new("Le casino de Scotty est organisé en zones d'intensité croissante. La périphérie est douce — des jeux légers, des boissons offertes, des distractions. Plus tu t'enfonces, plus les mises augmentent, plus le bruit diminue, plus les tables deviennent sérieuses. Au centre, quelques salles privées dont les portes ne s'ouvrent que pour certains. Scotty descend parfois dans ces salles. Jamais seul.", Color.Gold1),
            new("Les croupiers de Scotty Golden North sont formés à la psychologie autant qu'aux règles du jeu. Ils savent reconnaître quand quelqu'un perd plus qu'il ne devrait, quand la soirée bascule, quand il faut proposer un verre de plus ou au contraire appeler discrètement un collègue. La machine à profits la plus efficace se soucie de la santé de ses clients. Jusqu'à un certain point.", Color.Gold1),
            new("Samy Scotty est visible depuis son balcon à presque toutes les heures. Il observe. Pas par méfiance — par fascination. Il aime les gens qui jouent. Il aime leurs espoirs, leurs stratégies, leur façon de gérer la perte et la victoire. Il a construit cet endroit pour les regarder. L'argent est le prétexte.", Color.Gold1),
        ],

        "Star Quest" => [
            new("Star Quest vibre. La musique commence dès les sas d'entrée — pas agressive, juste présente, te préparant à quelque chose. Mister Eliotis a une théorie sur le tempo optimal pour la réception des nouvelles arrivées. Il l'a testée, ajustée, testée encore. Ce que tu entends est le résultat d'années de recherche hédonique.", Color.Gold1),
            new("La fête chez Eliotis n'est pas continue — elle est perpétuelle. Différents espaces, différentes ambiances, différentes intensités. Des cocktails dont les noms sont des mots inventés servant des attentes précises. Des lumières calibrées par heure de la nuit pour maintenir l'état d'esprit voulu. C'est de la manipulation consentie. Les gens reviennent.", Color.Gold1),
            new("Tu croises des gens de tous les secteurs dans les couloirs de Star Quest. Des pilotes militaires en permission, des marchands qui célèbrent une bonne affaire, des criminels qui fêtent quelque chose qu'ils ne peuvent pas nommer publiquement. Eliotis accueille tout le monde avec le même sourire. Il ne demande pas d'où on vient. Il demande ce qu'on veut.", Color.Gold1),
        ],

        "La Couronne d'Eos" => [
            new("La Couronne d'Eos est le centre politique le plus sophistiqué du secteur. Architecture imposante, couloirs ornés de représentations de l'histoire spatiale, personnels en tenues impeccables qui se déplacent avec une efficacité silencieuse. Le Président Eos a compris que le pouvoir s'exerce aussi visuellement. Tout ici est une déclaration.", Color.Gold1),
            new("Les visiteurs de la Couronne d'Eos sont filtrés. Plusieurs fois. La réputation compte, les antécédents comptent, l'équipement visible compte. Une machine à sourire à chaque point de contrôle — des agents de sécurité formés à être agréables tout en étant absolument inflexibles. Tu te demandes ce qui se passerait si tu insistais. Tu préfères ne pas vérifier.", Color.Gold1),
        ],

        "Emporium Requiem" => [
            new("Emporium Requiem est une planète artificielle. Ce n'est pas une métaphore. La structure a été construite pour ressembler à une planète — surface habitable, atmosphère maintenue, gravité simulée à 0.98g. Marcher ici, c'est marcher sur quelque chose que des gens ont construit de toutes pièces. L'Emporium est la démonstration la plus élaborée que l'argent change la nature de la réalité.", Color.Gold1),
            new("Les marchés de l'Emporium Requiem couvrent des kilomètres. Chaque niveau est une spécialité — matériaux, artefacts, information, accès. Les prix sont affichés en or parce que les crédits manquent de prestige ici. Des courtiers aux vêtements irréprochables circulent entre les étals avec des tablettes. Ils savent qui tu es avant que tu ouvres la bouche.", Color.Gold1),
            new("Il y a une règle à l'Emporium qu'on ne t'explique pas à l'entrée : la courtoisie est obligatoire. Pas par politesse — par convention. Les gens ici règlent leurs différends par d'autres moyens que la violence, et ces autres moyens sont souvent bien pires. Quand un marchand te sourit ici, c'est rarement de bonne humeur.", Color.Gold1),
            new("La douane de l'Emporium Requiem est réputée pour son inefficacité calculée. Les files d'attente sont longues, les vérifications minutieuses, les inspecteurs lents. C'est intentionnel — les délais génèrent des frais, les frais se négocient, les négociations se font à l'avantage de quelqu'un. Ce quelqu'un n'est pas toi.", Color.DarkOrange),
        ],

        // ── MILITAIRE ────────────────────────────────────────────────────────

        "La Citadelle Écarlate" => [
            new("La Citadelle Écarlate tire son nom des panneaux de revêtement en alliage rougeâtre qui couvrent ses parois extérieures. À l'intérieur, les couleurs sont plus neutres mais l'atmosphère reste martiale. Grand Gardien Sorath gouverne cet endroit avec une rigueur qui serait excessive n'importe où ailleurs. Ici elle est la norme.", Color.Red),
            new("Les Gardiens Écarlates qui patrouillent dans la Citadelle ont cet air de gens qui croient en quelque chose. Pas juste un emploi — une mission. Sorath a cultivé ça consciencieusement depuis des années. Une foi dans l'ordre, dans la protection, dans l'idée qu'une ligne peut être tenue si assez de gens décident de la tenir. C'est naïf et efficace en même temps.", Color.Red),
        ],

        "Fort Ossian" => [
            new("Fort Ossian est une machine de guerre parfaitement entretenue. Le Général Ossian a le culte du détail — chaque pièce d'équipement vérifiée, chaque protocole respecté, chaque recrue évaluée au-delà de ses propres attentes. Le fort ne décroche jamais. Même en temps de paix, l'état d'alerte est maintenu à un niveau qui permettrait de passer en opérationnel en quatre-vingt secondes.", Color.SteelBlue1),
            new("Le marché d'armement de Fort Ossian est légal, rigoureusement documenté et terriblement bien approvisionné. Armes de précision, équipements de combat avancés, protections balistiques de qualité militaire. Les prix sont plus élevés qu'ailleurs mais la garantie de qualité est absolue. Ossian ne laisse pas vendre de mauvais équipement sur ses terres. Il considère que c'est une question morale.", Color.SteelBlue1),
        ],

        "La Citadelle" => [
            new("La Citadelle ressemble à ce qu'elle est — un bunker habité. Construite pour résister, jamais reconvertie dans une vocation plus douce. Les murs sont épais, les accès réduits, les systèmes de défense intégrés à l'architecture comme une colonne vertébrale. Les gens qui vivent ici depuis longtemps ont la façon de marcher de quelqu'un qui s'attend à ce que les murs résistent.", Color.SteelBlue1),
        ],

        // ── LIEUX SPÉCIAUX ───────────────────────────────────────────────────

        "La République de Cellule 9" => [
            new("La République de Cellule 9 occupe l'aile nord d'une ancienne prison désaffectée. Le Président-Condamné a fondé son gouvernement dans sa cellule d'origine — les murs portent les quarante-sept articles de la Constitution qu'il a rédigée pendant ses années de détention. C'est théoriquement absurde et pratiquement fonctionnel. Quelques centaines de personnes ont décidé de croire à cet endroit.", Color.OrangeRed1),
            new("Les citoyens de la République de Cellule 9 ont tous en commun d'avoir quelque chose à fuir ou quelque chose à prouver. L'idéalisme du Président-Condamné les a attirés — l'idée qu'un gouvernement peut être juste même dans les conditions les moins propices. Si cette idée fonctionnerait à grande échelle, personne ne le sait encore. Ici, à petite échelle, elle tient.", Color.OrangeRed1),
        ],

        "Nid de Vorreth" => [
            new("Le Nid de Vorreth est une anomalie. Les Vorreth ne bâtissent pas à la manière humaine — les structures organiques qui constituent leur habitat ont poussé autour d'un noyau original selon des logiques de croissance que les architectes humains ne comprennent pas entièrement. L'espace intérieur est fonctionnel d'une façon étrangère, calibré pour des corps différents des tiens.", Color.Yellow),
            new("Le protocole d'approche du Nid est documenté par ceux qui l'ont négocié avec les Vorreth — gestes lents, pas d'objet réfléchissant tenu vers eux, aucun son au-dessus d'un certain seuil. Les Vorreth perçoivent le monde différemment. Respecter leurs sensibilités n'est pas de la politesse — c'est une condition de survie.", Color.Yellow),
        ],

        "La Station-Océan de Rhassil" => [
            new("La Station-Océan de Rhassil est recouverte d'eau — des centimètres, en permanence, sur tous les sols accessibles. Les Rhassil ont besoin de cette humidité constante et l'ont intégrée à toute leur architecture. Les humains qui y transitent portent des bottes imperméables ou acceptent d'avoir les pieds mouillés. Les Rhassil trouvent la deuxième option plus honnête.", Color.Cyan1),
        ],

        "Assemblée Première" => [
            new("Assemblée Première est habitée par des êtres que tu ne reconnais pas comme des êtres au premier regard. Les Premiers sont des intelligences distribuées incarnées dans des structures physiques variables. Ils ne ressemblent pas à ce que la culture humaine imagine quand elle pense à l'intelligence artificielle. Ils ressemblent à quelque chose que la culture humaine n'a pas encore imaginé.", Color.SteelBlue1),
        ],

        "Le Conclave des Ombres" => [
            new("Le Conclave des Ombres n'est pas signalé. Tu sais que tu y es arrivé quand l'éclairage change — plus sombre, plus directionnel, conçu pour créer des zones d'ombre profonde entre les zones éclairées. Dans les zones d'ombre, des gens font des affaires qui n'ont pas besoin de lumière. Les secrets se vendent ici. Certains secrets ne se vendent qu'une fois parce que leur valeur s'évapore dès leur divulgation.", Color.Grey),
        ],

        "Terminus Sud" => [
            new("Terminus Sud est l'endroit où les choses finissent. Les routes spatiales s'arrêtent ici, les cartes deviennent vagues, les services s'essoufflent. Yenna, l'informatrice locale, sait tout ce qu'il y a à savoir sur ce qui passe et ce qui ne passe pas. Elle vend cette information avec la précision de quelqu'un qui a compris que la géographie est un pouvoir.", Color.Grey),
            new("Le bar de Terminus Sud est le seul endroit où tout le monde finit par passer. Pilotes en bout de route, marchands dont l'itinéraire s'est compliqué, gens qui cherchent une dernière chance avant de décider quoi faire de leur vie. Le barman a entendu toutes les histoires. Il n'en répète aucune. C'est pour ça qu'il dure.", Color.Grey),
        ],

        "Le Phare de Vorn" => [
            new("Le Phare de Vorn émet un signal en continu. Pas une balise de navigation — une fréquence d'information encodée que seuls ceux qui savent écouter peuvent déchiffrer. Vorn a construit ce phare pour diffuser ce qu'il appelle 'la vérité non filtrée'. Ce qu'il diffuse exactement, c'est la question que tout le monde se pose avant de décider s'il veut lui rendre visite.", Color.Cyan1),
        ],

        "L'Arche de Sélène" => [
            new("L'Arche de Sélène est une station génération — initialement prévue pour un voyage interstellaire de cent ans qui n'a jamais eu lieu. Sélène, coordinatrice de l'Arche, a transformé une mission avortée en communauté permanente. Les habitants descendent de gens qui avaient accepté de ne jamais revoir leur monde d'origine. Cette acceptation est devenue culture.", Color.Green),
        ],

        "Station Terminus Noir" => [
            new("Station Terminus Noir n'est pas sur les cartes habituelles. Elle existe dans l'espace entre deux routes commerciales, assez proche pour être accessible, assez loin pour être ignorée par qui ne cherche pas spécifiquement. Ceux qui veulent disparaître viennent ici. Les services qui permettent de disparaître s'y sont naturellement développés.", Color.Grey),
        ],

        "L'Observatoire" => [
            new("L'Observatoire est construit autour d'un télescope de la taille d'un vaisseau moyen. L'Astronome Lyra l'a orienté vers une région du ciel que les autres astronomes ont décidé d'ignorer. Ce qu'elle y voit, elle ne le dit pas entièrement. Ce qu'elle dit est déjà suffisamment étrange pour que des gens fassent de longs détours pour l'entendre.", Color.Cyan1),
        ],

        "Le Marché des Damnés" => [
            new("Le Marché des Damnés est un vacarme organisé. Des milliers de stands, des milliers de voix, des milliers de transactions simultanées dans un espace qui n'a jamais été prévu pour autant de monde. L'odeur est un mélange de tout — nourriture, métal, chimie, humain. Les couloirs sont tracés au sens large et négociés à chaque passage. Tu gardes tes affaires près de ton corps.", Color.OrangeRed1),
            new("Dans les niveaux inférieurs du Marché des Damnés, là où les autorités ne vont pas, les prix sont différents et les questions absentes. Ce qui n'a pas de valeur en surface en a ici. Ce qui est illégal en surface est simplement cher ici. L'économie de la survie a ses propres règles. Tu y contribues à la seconde où tu franchis le seuil.", Color.Red),
        ],

        "La Forge des Damnés" => [
            new("La Forge des Damnés produit des armes que les catalogues officiels ne listent pas. L'Armurière Skade travaille à la commande, sur spécification, sans questions. Sa réputation s'est bâtie sur la discrétion autant que sur la qualité. Les armes qu'elle fabrique ne portent pas de numéros de série. Ce n'est pas un défaut — c'est une feature.", Color.OrangeRed1),
            new("Les travailleurs de la Forge ne sont pas tous là par choix. Certains ont des dettes, certains ont des familles quelque part que quelqu'un surveille, certains ont été achetés à d'autres situations encore pires. L'Armurière Skade tient ses ouvriers au travail par des mécanismes qu'elle n'explique pas. Libérer les travailleurs forcés est risqué. Ne rien faire l'est aussi, d'une autre façon.", Color.Red),
        ],

        "Station Belvédère" => [
            new("Station Belvédère a été construite en altitude — sur le pic d'un astéroïde qui domine les routes spatiales environnantes. Lord Cassen a choisi cet endroit précisément pour la vue et pour le symbole : regarder le monde depuis au-dessus. L'architecture de la station renforce cette idée — grandes baies vitrées, hauteurs de plafond excessives, espaces ouverts. Tout ici regarde dehors et vers le bas.", Color.Gold1),
        ],

        "L'Entrepôt Zéro" => [
            new("L'Entrepôt Zéro n'est pas une station — c'est un espace de stockage de la taille d'une station. Des rangées d'étagères qui montent jusqu'aux parois supérieures, des robots de manutention qui circulent selon des trajectoires précalculées, et au centre, une petite zone de bureau où Le Gérant règne sur tout ça avec une encyclopédique connaissance de chaque article stocké ici. Il connaît la valeur de tout.", Color.DarkOrange),
        ],

        "La Colonie Errante" => [
            new("La Colonie Errante n'a pas de position fixe. Elle dérive selon un itinéraire semi-prévisible qui mène les navigateurs expérimentés à l'intercepter à certains points de passage. Le Capitaine Vera a voulu ça — une communauté mobile par principe, attachée à aucun endroit, dépendante de personne. La liberté de mouvement comme philosophie politique.", Color.Cyan1),
        ],

        "Les Puits de Noctis" => [
            new("Les Puits de Noctis descendent loin dans le roc. Les galeries s'enfoncent à des niveaux où la pression et la chaleur rendent le travail épuisant, où la roche est d'une densité qui émousse les outils en heures. Les mineurs ici travaillent par rotation forcée parce que personne ne tiendrait plus d'une semaine continue dans les niveaux profonds. Le Directeur Pale calcule que la rotation est moins chère que le remplacement.", Color.DarkRed),
            new("La lumière naturelle n'arrive pas aux niveaux où travaillent les mineurs. Ils ne voient pas le ciel pendant des semaines. Certains finissent par ne plus en avoir besoin — leur cycle biologique s'adapte à la lumière artificielle, à l'air filtré, aux horaires imposés. C'est une forme d'adaptation que le Directeur Pale encourage activement. Des employés qui n'ont plus de référence extérieure sont des employés plus stables.", Color.DarkRed),
        ],

        "La Station du Jugement" => [
            new("La Station du Jugement est une anomalie architecturale : une seule grande salle centrale, des tribunes tout autour, un podium au milieu. Le Juge préside. Les gens viennent ici pour des raisons diverses — régler des disputes, chercher des réponses, être jugés pour des choses qu'ils ne peuvent pas porter seuls. Le Juge entend tout et prononce des verdicts que personne n'est obligé de respecter mais que beaucoup respectent quand même.", Color.Gold1),
        ],

        "L'Île Volante de Marris" => [
            new("Marris appelle ça une île. Techniquement c'est un vaisseau de transport reconverti qui a cessé de bouger un jour et autour duquel une structure s'est développée. L'île flotte dans un système de propulsion basse intensité qui maintient une position relative. Marris vend des choses qu'il décrit comme 'de nulle part et de partout'. La description est peut-être exacte.", Color.Cyan1),
        ],

        "Le Marché Flottant" => [
            new("Le Marché Flottant change de position toutes les semaines. C'est sa nature — une caravane de vaisseaux qui se retrouvent à des points convenus, forment un marché éphémère, puis se séparent. Hamid le Chanceux organise ça depuis vingt ans. Son système de communication codée permet aux habitués de savoir où il sera. Les non-habitués le trouvent par accident ou par recommandation.", Color.Gold1),
        ],

        _ => [
            new("Tu arrives dans cette partie de la station que les cartes ne décrivent pas avec précision. Les gens circulent, chacun dans sa direction, portant leur propre histoire sans la partager. Il y a dans tous ces endroits une façon d'exister simultanément ensemble et seul qui te frappe différemment selon les jours.", Color.Grey),
            new("Un endroit comme un autre, et pourtant pas comme un autre. Tu regardes les gens qui passent en te demandant ce que chacun cache dans sa façon de marcher.", Color.Grey),
            new("La station ronronne. Quelque part des machines font des choses. Quelque part des gens prennent des décisions. Ici, maintenant, tu traînes dans ce qui ressemble à l'endroit entre les événements.", Color.Grey),
        ],
    };
}
