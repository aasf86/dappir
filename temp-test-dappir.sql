 SELECT * FROM BensApreendidosProcesso  where BensApreendidosProcessoGUID = '837FA7DD-7748-46D9-A0EE-C221102EED5F';--44

 SELECT * FROM BensApreendidosItem where BensApreendidosProcessoGUID = '58DF9403-C9BC-44A7-BE4A-29CD067A5728';--69


  SELECT * FROM BensApreendidosMovimentoItem where BensApreendidosItemID = 69;

  select * from BensApreendidosMovimento where BensApreendidosMovimentoID in (SELECT BensApreendidosMovimentoID FROM BensApreendidosMovimentoItem where BensApreendidosItemID = 69);

--//

select
	mov.*
from
	BensApreendidosMovimento mov
where
	mov.BensApreendidosMovimentoID in (
		select
			movItem.BensApreendidosMovimentoID
		from
			BensApreendidosMovimentoItem movItem
			inner join BensApreendidosItem item on movItem.BensApreendidosItemID = item.BensApreendidosItemID
		where
			item.BensApreendidosProcessoID = 41
);


create table Pedido
(
	PedidoId int not null primary key identity,
	Descricao varchar(500)
);

create table Endereco
(
	EnderecoId int not null primary key identity,
	PedidoId  int not null references Pedido(PedidoId),
	Localizacao varchar(500)
);

create table PedidoItem
(
	PedidoItemId int not null primary key identity,
	PedidoId  int not null references Pedido(PedidoId),
	Descricao varchar(500)
);

select * from Pedido;
select * from Endereco;
select * from PedidoItem;

/*
--drop table Pedido;
--drop table Endereco;
--drop table PedidoItem;
*/

--commit;