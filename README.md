# Crear entorno con Python 3.11 (compatible con Mesa)
conda create --name mesa_project python=3.11

# Activar el entorno
conda activate mesa_project

# Desactivar entorno
conda deactivate 

# Instalar Mesa y sus dependencias de visualización
conda install -c conda-forge mesa
pip install --pre mesa[viz]  # Para SolaraViz

# Instalar librerías científicas básicas
conda install numpy pandas matplotlib
