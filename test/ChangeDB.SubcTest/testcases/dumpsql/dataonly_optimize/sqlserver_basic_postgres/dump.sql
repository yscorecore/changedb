COPY "dbo"."abc"("id", "val") FROM STDIN;
1	1
2	2
\.

SELECT SETVAL(PG_GET_SERIAL_SEQUENCE('"dbo"."abc"', 'id'), 2);
