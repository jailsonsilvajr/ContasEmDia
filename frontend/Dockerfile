# frontend/Dockerfile

# Estágio "build": imagem Node só para compilar os assets estáticos do
# Angular — não vai para a imagem final
FROM node:22-alpine AS build
WORKDIR /app

# Copia só os manifestos de dependências primeiro, para aproveitar o cache
# de camadas do Docker (o "npm ci" só reexecuta se esses arquivos mudarem)
COPY package.json package-lock.json ./
# Instala as dependências exatamente como travadas no package-lock.json
# (mais rápido e determinístico que "npm install" em CI/build)
RUN npm ci

# Copia o restante do código-fonte do frontend
COPY . .
# Gera o build de produção (minificado, otimizado) do Angular
RUN npm run build -- --configuration production

# Estágio "runtime": Nginx servindo só os arquivos estáticos gerados —
# imagem final bem menor, sem Node/NPM
FROM nginx:1.27-alpine AS runtime
# Copia apenas os artefatos compilados do estágio de build
COPY --from=build /app/dist/frontend/browser /usr/share/nginx/html
# Substitui a configuração padrão do Nginx pela configuração customizada
# (SPA fallback + proxy reverso para a API, definida abaixo)
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
