#!/usr/bin/env bash
# Sobe o Psycheflow (banco + API + front) no Docker para desenvolver. Uso: ./dev.sh [comando]
#
#   (nada) | up   sobe tudo, reconstruindo as imagens, e espera ficar pronto
#   watch         sobe tudo e reconstrói a API/o front ao salvar o código (Ctrl+C para de observar)
#   db            sobe só o banco (para rodar a API com `dotnet run` e o front com `npm run dev`)
#   logs [svc]    acompanha os logs (db, api ou web; sem nome, todos)
#   down          para os containers (os dados ficam)
#   reset         apaga o banco e os anexos e sobe tudo de novo com os dados de demonstração
set -euo pipefail

cd "$(dirname "$0")"

usage() { sed -n '2,9p' "$0" | sed 's/^# \{0,1\}//'; }

case "${1:-up}" in
  up | watch | db | logs | down | reset) ;;
  help | -h | --help) usage; exit 0 ;;
  *) usage >&2; exit 1 ;;
esac

if ! command -v docker >/dev/null 2>&1; then
  echo "Docker não encontrado. Instale o Docker Desktop: https://www.docker.com/products/docker-desktop/" >&2
  exit 1
fi
if ! docker info >/dev/null 2>&1; then
  echo "O Docker não está rodando. Abra o Docker Desktop e tente de novo." >&2
  exit 1
fi

if [ ! -f .env ]; then
  cp .env.example .env
  echo "Criei o .env a partir do .env.example (edite para mudar portas ou colocar chaves de IA)."
fi

# Valor de uma variável do .env (ou o padrão), só para mostrar os endereços.
env_value() {
  local value
  value=$(grep -E "^$1=" .env | tail -n 1 | cut -d= -f2- | tr -d '\r' || true)
  echo "${value:-$2}"
}

up() {
  docker compose up -d --build --wait \
    || { echo "Algo não subiu. Veja os logs com: ./dev.sh logs api" >&2; exit 1; }
  cat <<EOF

Psycheflow no ar:
  Front ......... http://localhost:$(env_value FRONT_PORT 5173)
  API (Scalar) .. http://localhost:$(env_value API_PORT 8080)/scalar
  Login demo .... ana@psycheflow.dev / Psycheflow@123
EOF
}

case "${1:-up}" in
  up)
    up
    ;;
  watch)
    up
    echo
    echo "Observando Psycheflow.Api/src e Psycheflow.Front/src (Ctrl+C para de observar; os containers continuam)."
    docker compose watch --no-up
    ;;
  db)
    docker compose up -d --wait db
    echo "Banco em localhost:$(env_value POSTGRES_PORT 5432)."
    echo "API: cd Psycheflow.Api && dotnet run --project src/Psycheflow.Api   (http://localhost:5240)"
    echo "Front: cd Psycheflow.Front && npm run dev   (VITE_API_URL=http://localhost:5240 no .env.local)"
    ;;
  logs)
    shift
    docker compose logs -f "$@"
    ;;
  down)
    docker compose down
    ;;
  reset)
    read -r -p "Isso apaga o banco e os anexos locais. Continuar? [s/N] " answer
    case "$answer" in
      s | S) ;;
      *) echo "Cancelado."; exit 0 ;;
    esac
    docker compose down -v
    up
    ;;
esac
