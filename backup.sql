--
-- PostgreSQL database dump
--

-- Dumped from database version 11.10
-- Dumped by pg_dump version 13.1

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

SET default_tablespace = '';

--
-- Name: users; Type: TABLE; Schema: public; Owner: user
--

CREATE TABLE public.users (
    id integer NOT NULL,
    userid integer NOT NULL,
    words integer[],
    ingame boolean DEFAULT false NOT NULL,
    violation integer DEFAULT 0 NOT NULL,
    answers character varying(30)[] DEFAULT NULL::character varying[]
);


ALTER TABLE public.users OWNER TO "user";

--
-- Name: users_id_seq; Type: SEQUENCE; Schema: public; Owner: user
--

CREATE SEQUENCE public.users_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.users_id_seq OWNER TO "user";

--
-- Name: users_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: user
--

ALTER SEQUENCE public.users_id_seq OWNED BY public.users.id;


--
-- Name: words; Type: TABLE; Schema: public; Owner: user
--

CREATE TABLE public.words (
    id integer NOT NULL,
    word character varying(30),
    definition text
);


ALTER TABLE public.words OWNER TO "user";

--
-- Name: words_id_seq; Type: SEQUENCE; Schema: public; Owner: user
--

CREATE SEQUENCE public.words_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.words_id_seq OWNER TO "user";

--
-- Name: words_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: user
--

ALTER SEQUENCE public.words_id_seq OWNED BY public.words.id;


--
-- Name: users id; Type: DEFAULT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.users ALTER COLUMN id SET DEFAULT nextval('public.users_id_seq'::regclass);


--
-- Name: words id; Type: DEFAULT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.words ALTER COLUMN id SET DEFAULT nextval('public.words_id_seq'::regclass);


--
-- Data for Name: users; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.users (id, userid, words, ingame, violation, answers) FROM stdin;
1	479404570	\N	f	0	\N
\.


--
-- Data for Name: words; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.words (id, word, definition) FROM stdin;
34118	інЕрція	\N
33873	агронОмія	
33874	алфАвІт	
33875	Аркушик	
33876	асиметрІя	
33877	багаторазОвий	
33878	безпринцИпний	
33879	бЕшкет	
33880	блАговіст	
33881	близькИй	
33882	болотИстий	
33883	борОдавка	
33884	босОніж	
33885	боЯзнь	
33886	бурштинОвий	
33887	бюлетЕнь	
33888	вАги	у множині
33889	вантажІвка	
33890	веснЯнИй	
33891	вИгода	користь
33892	вигОда	зручність
33893	видАння	
33894	визвОльний	
33895	вимОга	
33896	вИпадок	
33897	вирАзний	
33898	вИсіти	
33899	вИтрата	
33900	вишИваний	
33901	відвезтИ	
33902	відвестИ	
33903	вІдгомін	
33904	віднестИ	
33905	вІдомість	список
33906	відОмість	повідомлення, дані, популярність
33907	вІрші	
33908	віршовИй	
33909	вітчИм	
33910	гАльмО	
33911	глядАч	
33912	горошИна	
33913	граблІ	
33914	гуртОжиток	
33915	данИна	
33916	дАно	
33917	децимЕтр	
33918	дЕщиця	
33919	де-Юре	
33920	джерелО	
33921	дИвлячись	
33922	дичАвіти	
33923	діалОг	
33924	добовИй	
33925	добУток	
33926	довезтИ	
33927	довестИ	
33928	довІдник	
33929	дОгмат	
33930	донестИ	
33931	дОнька	
33932	дочкА	
33933	дрОва	
33934	експЕрт	
33935	єретИк	
33936	жалюзІ	
33937	завдАння	
33938	завезтИ	
33939	завестИ	
33940	зАвжди	
33941	завчасУ	
33942	зАгадка	
33943	заіржАвілий	
33944	заіржАвіти	
33945	закінчИти	
33946	зАкладка	у книзі
33947	зАкрутка	
33948	залишИти	
33949	замІжня	
33950	занестИ	
33951	зАпонка	
33952	заробІток	
33953	зАставка	
33954	зАстібка	
33955	застОпорити	
33956	звИсока	
33957	здАлека	
33958	зібрАння	
33959	зобразИти	
33960	зОзла	
33961	зрАння	
33962	зрУчний	
33963	зубОжіння	
33964	індУстрія	
33965	кАмбала	
33966	каталОг	
33967	квартАл	
33968	кИшка	
33969	кіломЕтр	
33970	кінчИти	
33971	кОлесо	
33972	кОлія	
33973	кОпчений	дієприкметник
33974	копчЕний	прикметник
33975	корИсний	
33976	кОсий	
33977	котрИй	
33978	крицЕвий	
33979	крОїти	
33980	кропивА	
33981	кулінАрія	
33982	кУрятина	
33983	лАте	
33984	листопАд	
33985	літОпис	
33986	лЮстро	
33987	мАбУть	
33988	магістЕрський	
33989	мАркетинг	
33990	мерЕжа	
33991	металУргія	
33992	мілімЕтр	
33993	навчАння	
33994	нанестИ	
33995	напІй	
33996	нАскрізний	
33997	нАчинка	
33998	ненАвидіти	
33999	ненАвисний	
34000	ненАвисть	
34001	нестИ	
34002	нІздря	
34003	новИй	
34004	обіцЯнка	
34005	обрАння	
34006	обрУч	іменник
34007	одинАдцять	
34008	одноразОвий	
34009	ознАка	
34010	Олень	
34011	оптОвий	
34012	осетЕр	
34013	отАман	
34014	Оцет	
34015	павИч	
34016	партЕр	
34017	пЕкарський	
34018	перевезтИ	
34019	перевестИ	
34020	перЕкис	
34021	перелЯк	
34022	перенестИ	
34023	перЕпад	
34024	перЕпис	
34025	піалА	
34026	пІдданий	дієприкметник
34027	піддАний	іменник, істота
34028	пІдлітковий	
34029	пізнАння	
34030	пітнИй	
34031	піцЕрія	
34032	пОдруга	
34033	пОзначка	
34034	пОмилка	
34035	помІщик	
34036	помОвчати	
34037	понЯття	
34038	порядкОвий	
34039	посерЕдині	
34040	привезтИ	
34041	привестИ	
34042	прИморозок	
34043	принестИ	
34044	прИчіп	
34045	прОділ	
34046	промІжок	
34047	псевдонІм	
34048	рАзом	
34049	рЕмінь	пояс
34050	рЕшето	
34051	рИнковий	
34052	рівнИна	
34053	роздрібнИй	
34054	рОзпірка	
34055	рукОпис	
34056	руслО	
34057	сантимЕтр	
34058	свЕрдло	
34059	серЕдина	
34060	сЕча	
34061	симетрІя	
34062	сільськогосподАрський	
34063	сімдесЯт	
34064	слИна	
34065	соломИнка	
34066	стАтуя	
34067	стовідсОтковий	
34068	стрибАти	
34069	текстовИй	
34070	течіЯ	
34071	тИгровий	
34072	тисОвий	
34073	тім’янИй	
34074	травестІя	
34075	тризУб	
34076	тУлуб	
34077	украЇнський	
34078	уподОбання	
34079	урочИстий	
34080	усерЕдині	
34081	фартУх	
34082	фаховИй	
34083	фенОмен	
34084	фОльга	
34085	фОрзац	
34086	хАос	у міфології: стихія
34087	хаОс	безлад
34088	цАрина	
34089	цемЕнт	
34090	цЕнтнер	
34091	ціннИк	
34092	чарівнИй	
34093	черговИй	
34094	читАння	
34095	чорнОзем	
34096	чорнОслив	
34097	чотирнАдцять	
34098	шляхопровІд	
34099	шовкОвий	
34100	шофЕр	
34101	щЕлепа	
34102	щИпці	
34103	щодобовИй	
34104	ярмаркОвий	
\.


--
-- Name: users_id_seq; Type: SEQUENCE SET; Schema: public; Owner: user
--

SELECT pg_catalog.setval('public.users_id_seq', 23, true);


--
-- Name: words_id_seq; Type: SEQUENCE SET; Schema: public; Owner: user
--

SELECT pg_catalog.setval('public.words_id_seq', 34118, true);


--
-- Name: users users_userid_key; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT users_userid_key UNIQUE (userid);


--
-- Name: words word_id; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.words
    ADD CONSTRAINT word_id UNIQUE (word);


--
-- PostgreSQL database dump complete
--

