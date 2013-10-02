create table __buildmaster_dbschemachanges (
  numeric_release_number bigint not null,
  script_id int not null,
  script_name varchar(50) not null,
  executed_date timestamp not null,
  success_indicator char(1) not null,

  constraint __buildmaster_dbschemachangespk
	primary key (numeric_release_number, script_id)
)
;

insert into __buildmaster_dbschemachanges
	(numeric_release_number, script_id, script_name, executed_date, success_indicator)
values
	(0, 0, 'create table __buildmaster_dbschemachanges', now(), 'Y')
;