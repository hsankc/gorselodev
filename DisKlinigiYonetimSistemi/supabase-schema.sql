create table if not exists public.clinic_data (
    id text primary key,
    payload jsonb not null,
    updated_at timestamptz not null default now()
);

alter table public.clinic_data enable row level security;

drop policy if exists "clinic_data_select" on public.clinic_data;
drop policy if exists "clinic_data_insert" on public.clinic_data;
drop policy if exists "clinic_data_update" on public.clinic_data;

create policy "clinic_data_select"
on public.clinic_data
for select
using (true);

create policy "clinic_data_insert"
on public.clinic_data
for insert
with check (true);

create policy "clinic_data_update"
on public.clinic_data
for update
using (true)
with check (true);

insert into public.clinic_data (id, payload)
values ('default', '{}'::jsonb)
on conflict (id) do nothing;
