#!/bin/bash
# Create demo data for Photo Studio using the API

set -e

echo "🎬 Creating Photo Studio Demo Data..."

# Login as studio owner
echo "📝 Creating studio account..."
RESPONSE=$(curl -s -X POST http://localhost:5001/api/DevAuth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"studio@sivar.os"}' \
  --cookie-jar /tmp/studio-cookies.txt)

echo "$RESPONSE" | jq .

if echo "$RESPONSE" | grep -q "success.*true"; then
    echo "✅ Studio account created/logged in"
else
    echo "❌ Failed to create studio account"
    exit 1
fi

echo ""
echo "📸 Creating demo posts..."

# Post 1: Wedding photography
curl -s -X POST http://localhost:5001/api/Posts \
  -H "Content-Type: application/json" \
  --cookie /tmp/studio-cookies.txt \
  -d '{
    "content": "¡Capturamos los momentos más especiales de tu boda! 💍✨\n\nNuestro equipo de fotógrafos profesionales tiene más de 10 años de experiencia documentando bodas inolvidables.\n\nServicios incluidos:\n✅ Fotografía profesional (hasta 8 horas)\n✅ 300+ fotos editadas\n✅ Álbum digital HD\n✅ Video resumen de 5 minutos\n✅ Sesión pre-boda incluida\n\nReserva tu fecha ahora. Cupos limitados para 2026.\n\n#BodaSV #FotografíaProfesional #ElSalvador",
    "visibility": 0
  }' | jq -r '.id // "Error"' && echo "✅ Post 1 created"

# Post 2: Quinceañera package
curl -s -X POST http://localhost:5001/api/Posts \
  -H "Content-Type: application/json" \
  --cookie /tmp/studio-cookies.txt \
  -d '{
    "content": "🎀 Paquete Quinceañera 2026 🎀\n\nHaz de tu quinceañera un día inolvidable con nuestro paquete completo:\n\n📸 Sesión de fotos en estudio\n📸 Fotos en locación (parque o playa)\n🎥 Video cinématico profesional\n💿 USB con todas las fotos (200+)\n📖 Álbum impreso de 20x30 cm\n\n¡Pregunta por nuestras promociones especiales!\n\nWhatsApp: +503 2222-3333\n\n#Quinceañera #XV #FiestaSV",
    "visibility": 0
  }' | jq -r '.id // "Error"' && echo "✅ Post 2 created"

# Post 3: Corporate photography
curl -s -X POST http://localhost:5001/api/Posts \
  -H "Content-Type: application/json" \
  --cookie /tmp/studio-cookies.txt \
  -d '{
    "content": "👔 Fotografía Corporativa y de Producto 👔\n\nServicios profesionales para empresas:\n\n• Retratos corporativos\n• Fotografía de producto\n• Eventos empresariales\n• Cobertura de conferencias\n• Fotos de equipo\n\nContamos con estudio equipado y equipo portátil para eventos en su empresa.\n\nSolicita una cotización sin compromiso.\n\n📧 info@studiophoto.sv\n📱 +503 2222-3333\n\n#FotografíaCorporativa #EmpresasSV #Marketing",
    "visibility": 0
  }' | jq -r '.id // "Error"' && echo "✅ Post 3 created"

# Post 4: Testimonial
curl -s -X POST http://localhost:5001/api/Posts \
  -H "Content-Type: application/json" \
  --cookie /tmp/studio-cookies.txt \
  -d '{
    "content": "⭐⭐⭐⭐⭐\n\n\"El servicio fue increíble. Las fotos de nuestra boda quedaron hermosas y el equipo fue muy profesional. ¡Totalmente recomendado!\"\n\n- María & Carlos (Boda Diciembre 2025)\n\nGracias por confiar en nosotros para capturar su día especial. 💕\n\n¿Quieres ver más testimoniales? Visita nuestro perfil.\n\n#TestimonioReal #ClientesFelices #BodaSV",
    "visibility": 0
  }' | jq -r '.id // "Error"' && echo "✅ Post 4 created"

echo ""
echo "✅ Demo data creation complete!"
echo ""
echo "📊 Test the feed:"
echo "curl -s http://localhost:5001/api/Posts/activity-feed --cookie /tmp/studio-cookies.txt | jq ."
