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