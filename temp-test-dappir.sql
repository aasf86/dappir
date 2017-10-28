--drop table Endereco;
--drop table PedidoItem;
--drop table Pedido;

create table Pedido
(
	PedidoId int not null primary key identity,
	Descricao varchar(max)
);

create table Endereco
(
	EnderecoId int not null primary key identity,
	PedidoId  int not null references Pedido(PedidoId),
	Localizacao varchar(max)
);

create table PedidoItem
(
	PedidoItemId int not null primary key identity,
	PedidoId  int not null references Pedido(PedidoId),
	Descricao varchar(max)
);

select * from Pedido;
select * from Endereco;
select * from PedidoItem;

/*
--drop table Pedido;
--drop table Endereco;
--drop table PedidoItem;
*/