docker run --name electronix-db \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=electronix \
  -p 5432:5432 \
  -d postgres