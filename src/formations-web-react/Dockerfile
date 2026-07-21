# ── Étape 1 : build de l'app React (Vite + TypeScript) ───────────────────────
FROM node:22-alpine AS build
WORKDIR /app

# VITE_API_URL est injectée au build (les vars Vite sont figées à la compilation).
# Le navigateur du client doit pouvoir joindre cette URL (donc localhost:5000 en dev).
ARG VITE_API_URL=http://localhost:5000
ENV VITE_API_URL=$VITE_API_URL

COPY package*.json ./
# npm install (et non npm ci) car le package-lock.json est généré dans l'image,
# rien n'étant installé sur la machine hôte.
RUN npm install

COPY . .
RUN npm run build

# ── Étape 2 : service statique via nginx ─────────────────────────────────────
FROM nginx:1.27-alpine AS final
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 8080
CMD ["nginx", "-g", "daemon off;"]
