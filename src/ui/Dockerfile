FROM ubuntu

RUN apt-get update && apt-get install -y nginx

COPY index.html /var/www/html/index.html
COPY jquery-3.3.1.min.js /var/www/html/jquery-3.3.1.min.js

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]